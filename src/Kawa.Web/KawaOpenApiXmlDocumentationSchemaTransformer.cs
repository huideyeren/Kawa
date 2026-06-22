using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kawa.Web;

/// <summary>
/// Adds XML documentation summaries from contract assemblies to generated OpenAPI schemas.
/// </summary>
internal sealed class KawaOpenApiXmlDocumentationSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly ConcurrentDictionary<Assembly, XmlDocumentationIndex> DocumentationByAssembly = new();

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (schema.Description is not null)
        {
            return Task.CompletedTask;
        }

        if (context.JsonPropertyInfo?.AttributeProvider is PropertyInfo propertyInfo)
        {
            schema.Description = GetDocumentation(propertyInfo.DeclaringType?.Assembly)
                .GetPropertySummary(propertyInfo);
        }
        else
        {
            var type = context.JsonTypeInfo.Type;
            schema.Description = GetDocumentation(type.Assembly).GetTypeSummary(type);
        }

        return Task.CompletedTask;
    }

    private static XmlDocumentationIndex GetDocumentation(Assembly? assembly)
    {
        return assembly is null
            ? XmlDocumentationIndex.Empty
            : DocumentationByAssembly.GetOrAdd(assembly, XmlDocumentationIndex.Load);
    }

    private sealed class XmlDocumentationIndex
    {
        private readonly IReadOnlyDictionary<string, string> summaries;

        private XmlDocumentationIndex(IReadOnlyDictionary<string, string> summaries)
        {
            this.summaries = summaries;
        }

        public static XmlDocumentationIndex Empty { get; } = new(new Dictionary<string, string>());

        public string? GetTypeSummary(Type type)
        {
            return summaries.GetValueOrDefault($"T:{GetDocumentationTypeName(type)}");
        }

        public string? GetPropertySummary(PropertyInfo propertyInfo)
        {
            var declaringType = propertyInfo.DeclaringType;
            if (declaringType is null)
            {
                return null;
            }

            return summaries.GetValueOrDefault(
                $"P:{GetDocumentationTypeName(declaringType)}.{propertyInfo.Name}");
        }

        public static XmlDocumentationIndex Load(Assembly assembly)
        {
            var documentationPath = GetDocumentationPath(assembly);
            if (documentationPath is null || !File.Exists(documentationPath))
            {
                return Empty;
            }

            try
            {
                using var reader = XmlReader.Create(
                    documentationPath,
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
                var document = XDocument.Load(reader);
                var summaries = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var member in document.Root?.Element("members")?.Elements("member") ?? [])
                {
                    var documentationId = member.Attribute("name")?.Value;
                    var summary = NormalizeSummary(member.Element("summary")?.Value);
                    if (documentationId is not null && summary is not null)
                    {
                        summaries[documentationId] = summary;
                    }
                }

                return new XmlDocumentationIndex(summaries);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
            {
                // OpenAPI generation must remain available when optional documentation files cannot be read.
                return Empty;
            }
        }

        private static string? GetDocumentationPath(Assembly assembly)
        {
            try
            {
                return string.IsNullOrWhiteSpace(assembly.Location)
                    ? null
                    : Path.ChangeExtension(assembly.Location, ".xml");
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static string GetDocumentationTypeName(Type type)
        {
            var documentationType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            return documentationType.FullName?.Replace('+', '.') ?? documentationType.Name;
        }

        private static string? NormalizeSummary(string? summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return null;
            }

            return string.Join(
                ' ',
                summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
