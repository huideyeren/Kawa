using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
    private static readonly ConcurrentDictionary<Assembly, KawaXmlDocumentationIndex> DocumentationByAssembly = new();

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
            schema.Description = GetDocumentation(propertyInfo.DeclaringType!.Assembly)
                .GetPropertySummary(propertyInfo);
        }
        else
        {
            var type = context.JsonTypeInfo.Type;
            schema.Description = GetDocumentation(type.Assembly).GetTypeSummary(type);
        }

        return Task.CompletedTask;
    }

    private static KawaXmlDocumentationIndex GetDocumentation(Assembly assembly)
    {
        return DocumentationByAssembly.GetOrAdd(assembly, KawaXmlDocumentationIndex.Load);
    }
}

/// <summary>
/// Indexes the XML documentation summaries emitted next to a contract assembly.
/// </summary>
internal sealed class KawaXmlDocumentationIndex
{
    private readonly IReadOnlyDictionary<string, string> summaries;

    private KawaXmlDocumentationIndex(IReadOnlyDictionary<string, string> summaries)
    {
        this.summaries = summaries;
    }

    internal static KawaXmlDocumentationIndex Empty { get; } = new(new Dictionary<string, string>());

    internal string? GetTypeSummary(Type type)
    {
        return summaries.GetValueOrDefault($"T:{GetDocumentationTypeName(type)}");
    }

    internal string? GetPropertySummary(PropertyInfo propertyInfo)
    {
        return summaries.GetValueOrDefault(
            $"P:{GetDocumentationTypeName(propertyInfo.DeclaringType!)}.{propertyInfo.Name}");
    }

    internal static KawaXmlDocumentationIndex Load(Assembly assembly)
    {
        var assemblyLocation = assembly.Location;
        var documentationPath = string.IsNullOrWhiteSpace(assemblyLocation)
            ? null
            : Path.ChangeExtension(assemblyLocation, ".xml");

        return Load(documentationPath);
    }

    internal static KawaXmlDocumentationIndex Load(string? documentationPath)
    {
        if (documentationPath is null || !File.Exists(documentationPath))
        {
            return Empty;
        }

        var document = TryLoadDocument(documentationPath);
        if (document is null)
        {
            return Empty;
        }

        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in document.Root!.Element("members")?.Elements("member") ?? [])
        {
            var documentationId = member.Attribute("name")?.Value;
            var summary = NormalizeSummary(member.Element("summary")?.Value);
            if (documentationId is not null && summary is not null)
            {
                summaries[documentationId] = summary;
            }
        }

        return new KawaXmlDocumentationIndex(summaries);
    }

    internal static string GetDocumentationTypeName(Type type)
    {
        var documentationType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return documentationType.FullName!.Replace('+', '.');
    }

    internal static string? NormalizeSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        return string.Join(
            ' ',
            summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    [ExcludeFromCodeCoverage(Justification = "Filesystem and XML parser failures vary by runtime and platform.")]
    private static XDocument? TryLoadDocument(string documentationPath)
    {
        try
        {
            using var reader = XmlReader.Create(
                documentationPath,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            return XDocument.Load(reader);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
        {
            // OpenAPI generation must remain available when optional documentation files cannot be read.
            return null;
        }
    }
}
