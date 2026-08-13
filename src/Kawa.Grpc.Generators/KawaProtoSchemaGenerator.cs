using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Kawa.Grpc.Generators;

/// <summary>
/// Generates deterministic protobuf schemas from an already-discovered Kawa service model.
/// </summary>
/// <remarks>
/// The MSBuild task owns source discovery. Keeping schema rendering independent lets the task
/// reject incompatible field-number reuse before invoking <c>Grpc.Tools</c>.
/// </remarks>
public static class KawaProtoSchemaGenerator
{
    /// <summary>
    /// Generates a protobuf schema for the supplied Kawa service definitions.
    /// </summary>
    /// <param name="packageName">The protobuf package name.</param>
    /// <param name="services">The Kawa services and their request and response messages.</param>
    /// <param name="csharpNamespace">The separate namespace used by generated C# protobuf types.</param>
    /// <returns>The rendered schema together with any protocol diagnostics.</returns>
    public static KawaProtoSchemaGenerationResult Generate(string packageName, IReadOnlyList<KawaProtoServiceDefinition> services, string? csharpNamespace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        ArgumentNullException.ThrowIfNull(services);

        var diagnostics = ValidateFieldNumbers(services);
        if (diagnostics.Count > 0)
        {
            return new KawaProtoSchemaGenerationResult(string.Empty, diagnostics);
        }

        var messages = services
            .SelectMany(static service => new[] { service.Request, service.Response })
            .GroupBy(static message => message.Name, StringComparer.Ordinal)
            .Select(static group => group.First())
            .OrderBy(static message => message.Name, StringComparer.Ordinal)
            .ToArray();

        var schema = new StringBuilder();
        schema.AppendLine("syntax = \"proto3\";");
        schema.AppendLine();
        schema.Append("package ").Append(packageName).AppendLine(";");
        if (!string.IsNullOrWhiteSpace(csharpNamespace))
        {
            schema.Append("option csharp_namespace = \"").Append(csharpNamespace).AppendLine("\";");
        }

        schema.AppendLine();

        foreach (var message in messages)
        {
            schema.Append("message ").Append(message.Name).AppendLine(" {");
            foreach (var field in message.Fields.OrderBy(static field => field.Number))
            {
                schema.Append("  ").Append(field.Type).Append(' ').Append(field.Name).Append(" = ").Append(field.Number).AppendLine(";");
            }

            schema.AppendLine("}");
            schema.AppendLine();
        }

        foreach (var service in services.OrderBy(static service => service.Name, StringComparer.Ordinal))
        {
            schema.Append("service ").Append(service.Name).AppendLine(" {");
            schema.Append("  rpc ").Append(service.OperationName).Append('(').Append(service.Request.Name).Append(") returns (").Append(service.Response.Name).AppendLine(");");
            schema.AppendLine("}");
            schema.AppendLine();
        }

        return new KawaProtoSchemaGenerationResult(schema.ToString(), diagnostics);
    }

    private static IReadOnlyList<KawaProtoDiagnostic> ValidateFieldNumbers(IReadOnlyList<KawaProtoServiceDefinition> services)
    {
        var diagnostics = new List<KawaProtoDiagnostic>();
        var messages = services
            .SelectMany(static service => new[] { service.Request, service.Response })
            .GroupBy(static message => message.Name, StringComparer.Ordinal)
            .Select(static group => group.First());

        foreach (var message in messages)
        {
            foreach (var duplicate in message.Fields.GroupBy(static field => field.Number).Where(static group => group.Count() > 1))
            {
                diagnostics.Add(new KawaProtoDiagnostic(
                    "KAWA5101",
                    $"Message '{message.Name}' reuses protobuf field number {duplicate.Key}. Field numbers must be unique and stable."));
            }
        }

        return diagnostics;
    }
}

/// <summary>
/// Writes a generated protobuf schema to an MSBuild intermediate-output path.
/// </summary>
/// <remarks>
/// Projects invoke this task before <c>Grpc.Tools</c>. The task intentionally accepts an output
/// path from MSBuild and never chooses a source-controlled directory itself.
/// </remarks>
public sealed class KawaProtoSchemaGenerationTask : Microsoft.Build.Utilities.Task
{
    private static readonly Regex ServiceExpression = new(
        "\\[KawaProtoService\\(\\\"(?<service>[^\\\"]+)\\\"\\)\\]\\s*public\\s+(?:sealed\\s+)?class\\s+(?<useCase>\\w+)\\s*:\\s*IUseCase<(?<request>\\w+),\\s*(?<response>\\w+)>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ContractExpression = new(
        "public\\s+(?:sealed\\s+)?class\\s+(?<name>\\w+)\\s*\\{(?<body>.*?)\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex FieldExpression = new(
        "\\[KawaProtoField\\((?<number>\\d+)\\)\\]\\s*public\\s+(?<type>[\\w\\[\\]]+)\\s+(?<name>\\w+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    /// <summary>
    /// Gets or sets the protobuf package name.
    /// </summary>
    [Required]
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the intermediate-output path for the generated schema.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the separate C# namespace used for generated protobuf types.
    /// </summary>
    public string CSharpNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace containing the Kawa use cases and contract types.
    /// </summary>
    public string ProjectNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the intermediate-output path for generated gRPC server adapters.
    /// </summary>
    public string AdapterOutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the services to render.
    /// </summary>
    public IReadOnlyList<KawaProtoServiceDefinition> Services { get; set; } = Array.Empty<KawaProtoServiceDefinition>();

    /// <summary>
    /// Gets or sets MSBuild service items. Each item identity is the service name and requires
    /// <c>OperationName</c>, <c>RequestName</c>, <c>ResponseName</c>, <c>RequestFields</c>, and
    /// <c>ResponseFields</c> metadata. Field metadata uses <c>name:type:number</c> entries
    /// separated by <c>|</c> so the build stays independent of source-controlled proto files.
    /// </summary>
    public ITaskItem[] ServiceDefinitions { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Gets or sets the project source files that may contain annotated Kawa use cases.
    /// </summary>
    public ITaskItem[] SourceFiles { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Gets the diagnostics from the most recent task execution.
    /// </summary>
    public IReadOnlyList<KawaProtoDiagnostic> Diagnostics { get; private set; } = Array.Empty<KawaProtoDiagnostic>();

    /// <summary>
    /// Gets the generated schema path for consumption by <c>Grpc.Tools</c>.
    /// </summary>
    [Output]
    public string GeneratedSchemaPath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the generated adapter source path for inclusion in the consuming project compilation.
    /// </summary>
    [Output]
    public string GeneratedAdapterSourcePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the input contained Kawa protobuf services.
    /// </summary>
    [Output]
    public bool HasServices { get; private set; }

    /// <summary>
    /// Generates and writes the protobuf schema.
    /// </summary>
    /// <returns><see langword="true"/> when schema generation succeeded; otherwise <see langword="false"/>.</returns>
    public override bool Execute()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PackageName);
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputPath);

        IReadOnlyList<KawaProtoServiceDefinition> definitions;
        try
        {
            definitions = ServiceDefinitions.Length != 0
                ? CreateServiceDefinitions(ServiceDefinitions)
                : SourceFiles.Length != 0
                    ? CreateServiceDefinitionsFromSource(SourceFiles)
                    : Services;
        }
        catch (KawaProtoSchemaException exception)
        {
            Diagnostics = [exception.Diagnostic];
            LogDiagnostics(Diagnostics);
            return false;
        }
        HasServices = definitions.Count != 0;
        if (!HasServices)
        {
            GeneratedSchemaPath = string.Empty;
            GeneratedAdapterSourcePath = string.Empty;
            Diagnostics = Array.Empty<KawaProtoDiagnostic>();
            return true;
        }

        var result = KawaProtoSchemaGenerator.Generate(PackageName, definitions, CSharpNamespace);
        Diagnostics = result.Diagnostics;
        if (result.Diagnostics.Count > 0)
        {
            LogDiagnostics(result.Diagnostics);
            return false;
        }

        var outputDirectory = Path.GetDirectoryName(OutputPath);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new ArgumentException("The schema output path must include a directory.", nameof(OutputPath));
        }

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(OutputPath, result.Schema, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        GeneratedSchemaPath = OutputPath;
        if (!string.IsNullOrWhiteSpace(AdapterOutputPath))
        {
            WriteAdapters(definitions);
        }

        return true;
    }

    private void WriteAdapters(IReadOnlyList<KawaProtoServiceDefinition> services)
    {
        if (string.IsNullOrWhiteSpace(AdapterOutputPath))
        {
            throw new ArgumentException("The adapter output path is required when Kawa protobuf services are present.", nameof(AdapterOutputPath));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ProjectNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(CSharpNamespace);

        var outputDirectory = Path.GetDirectoryName(AdapterOutputPath);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new ArgumentException("The adapter output path must include a directory.", nameof(AdapterOutputPath));
        }

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(AdapterOutputPath, RenderAdapters(services), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        GeneratedAdapterSourcePath = AdapterOutputPath;
    }

    private string RenderAdapters(IReadOnlyList<KawaProtoServiceDefinition> services)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.Append("namespace ").Append(ProjectNamespace).AppendLine(".Generated;");
        source.AppendLine();

        foreach (var service in services.OrderBy(static service => service.Name, StringComparer.Ordinal))
        {
            var adapterName = service.OperationName + "GrpcAdapter";
            var requestType = ProjectNamespace + "." + service.Request.Name;
            var responseType = ProjectNamespace + "." + service.Response.Name;
            var protoRequestType = CSharpNamespace + "." + service.Request.Name;
            var protoResponseType = CSharpNamespace + "." + service.Response.Name;
            source.Append("public sealed class ").Append(adapterName).Append(" : global::").Append(CSharpNamespace).Append('.').Append(service.Name).Append(".").Append(service.Name).AppendLine("Base");
            source.AppendLine("{");
            source.Append("    private readonly global::Kawa.Abstractions.IUseCase<global::").Append(requestType).Append(", global::").Append(responseType).AppendLine("> _useCase;");
            source.AppendLine("    private readonly global::Kawa.Core.UseCaseExecutor _executor;");
            source.Append("    public ").Append(adapterName).Append("(global::Kawa.Abstractions.IUseCase<global::").Append(requestType).Append(", global::").Append(responseType).Append("> useCase, global::Kawa.Core.UseCaseExecutor executor)").AppendLine();
            source.AppendLine("    {");
            source.AppendLine("        _useCase = useCase; _executor = executor;");
            source.AppendLine("    }");
            source.Append("    public override async global::System.Threading.Tasks.Task<global::").Append(protoResponseType).Append("> ").Append(service.OperationName).Append("(global::").Append(protoRequestType).AppendLine(" request, global::Grpc.Core.ServerCallContext context)");
            source.AppendLine("    {");
            source.Append("        var result = await _executor.ExecuteAsync(_useCase, new global::").Append(requestType).AppendLine("()");
            source.AppendLine("        {");
            foreach (var field in service.Request.Fields)
            {
                source.Append("            ").Append(ToPropertyName(field.Name)).Append(" = request.").Append(ToPropertyName(field.Name)).AppendLine(",");
            }

            source.AppendLine("        }, context.CancellationToken).ConfigureAwait(false);");
            source.AppendLine("        if (result.IsFailure) throw global::Kawa.Grpc.KawaGrpcErrorMapper.ToRpcException(result.Error!);");
            source.Append("        return new global::").Append(protoResponseType).AppendLine("()");
            source.AppendLine("        {");
            foreach (var field in service.Response.Fields)
            {
                source.Append("            ").Append(ToPropertyName(field.Name)).Append(" = result.Value!.").Append(ToPropertyName(field.Name)).AppendLine(",");
            }

            source.AppendLine("        };");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine();
        }

        return source.ToString();
    }

    private static IReadOnlyList<KawaProtoServiceDefinition> CreateServiceDefinitions(IEnumerable<ITaskItem> items)
    {
        return items.Select(item => new KawaProtoServiceDefinition(
            item.ItemSpec,
            GetRequiredMetadata(item, "OperationName"),
            new KawaProtoMessageDefinition(GetRequiredMetadata(item, "RequestName"), ParseFields(item, "RequestFields")),
            new KawaProtoMessageDefinition(GetRequiredMetadata(item, "ResponseName"), ParseFields(item, "ResponseFields"))))
            .ToArray();
    }

    private void LogDiagnostics(IEnumerable<KawaProtoDiagnostic> diagnostics)
    {
        if (BuildEngine is null)
        {
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            Log.LogError("{0}: {1}", diagnostic.Id, diagnostic.Message);
        }
    }

    private static IReadOnlyList<KawaProtoServiceDefinition> CreateServiceDefinitionsFromSource(IEnumerable<ITaskItem> sourceFiles)
    {
        var source = string.Join(Environment.NewLine, sourceFiles.Select(file => File.ReadAllText(file.ItemSpec)));
        var contracts = ContractExpression.Matches(source)
            .Cast<Match>()
            .ToDictionary(match => match.Groups["name"].Value, CreateMessage, StringComparer.Ordinal);

        return ServiceExpression.Matches(source)
            .Cast<Match>()
            .Select(match => new KawaProtoServiceDefinition(
                match.Groups["service"].Value,
                RemoveUseCaseSuffix(match.Groups["useCase"].Value),
                contracts[match.Groups["request"].Value],
                contracts[match.Groups["response"].Value]))
            .ToArray();
    }

    private static KawaProtoMessageDefinition CreateMessage(Match match)
    {
        var body = match.Groups["body"].Value;
        var fields = FieldExpression.Matches(body)
            .Cast<Match>()
            .Select(field => new KawaProtoFieldDefinition(
                ToFieldName(field.Groups["name"].Value),
                ToProtoType(field.Groups["type"].Value),
                int.Parse(field.Groups["number"].Value, System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();

        var publicProperties = Regex.Matches(body, "public\\s+[\\w\\[\\]]+\\s+\\w+\\s*\\{", RegexOptions.CultureInvariant);
        if (fields.Length != publicProperties.Count)
        {
            throw new KawaProtoSchemaException(new KawaProtoDiagnostic(
                "KAWA5102",
                $"Message '{match.Groups["name"].Value}' has a public member without KawaProtoField. Every serialized member requires an explicit stable field number."));
        }

        return new KawaProtoMessageDefinition(match.Groups["name"].Value, fields);
    }

    private static string RemoveUseCaseSuffix(string name) => name.EndsWith("UseCase", StringComparison.Ordinal)
        ? name[..^"UseCase".Length]
        : name;

    private static string ToFieldName(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    private static string ToPropertyName(string name) => char.ToUpperInvariant(name[0]) + name[1..];

    private static string ToProtoType(string type) => type switch
    {
        "string" => "string",
        "bool" => "bool",
        "int" => "int32",
        "long" => "int64",
        "float" => "float",
        "double" => "double",
        "byte[]" => "bytes",
        _ => type,
    };

    private static string GetRequiredMetadata(ITaskItem item, string metadataName)
    {
        var value = item.GetMetadata(metadataName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"KawaProtoService '{item.ItemSpec}' requires {metadataName} metadata.", nameof(item));
        }

        return value;
    }

    private static IReadOnlyList<KawaProtoFieldDefinition> ParseFields(ITaskItem item, string metadataName)
    {
        return GetRequiredMetadata(item, metadataName)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(field => field.Split(':', StringSplitOptions.TrimEntries))
            .Select(parts => parts.Length == 3 && int.TryParse(parts[2], out var number)
                ? new KawaProtoFieldDefinition(parts[0], parts[1], number)
                : throw new ArgumentException($"KawaProtoService '{item.ItemSpec}' has invalid {metadataName} field '{string.Join(':', parts)}'.", nameof(item)))
            .ToArray();
    }
}

internal sealed class KawaProtoSchemaException : Exception
{
    public KawaProtoSchemaException(KawaProtoDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public KawaProtoDiagnostic Diagnostic { get; }
}

/// <summary>
/// Defines a Kawa unary service operation for protobuf schema generation.
/// </summary>
/// <param name="Name">The protobuf service name.</param>
/// <param name="OperationName">The protobuf RPC operation name.</param>
/// <param name="Request">The request message.</param>
/// <param name="Response">The response message.</param>
public sealed record KawaProtoServiceDefinition(
    string Name,
    string OperationName,
    KawaProtoMessageDefinition Request,
    KawaProtoMessageDefinition Response);

/// <summary>
/// Defines a protobuf message derived from a Kawa contract.
/// </summary>
/// <param name="Name">The protobuf message name.</param>
/// <param name="Fields">The explicitly numbered message fields.</param>
public sealed record KawaProtoMessageDefinition(string Name, IReadOnlyList<KawaProtoFieldDefinition> Fields);

/// <summary>
/// Defines an explicitly numbered protobuf field.
/// </summary>
/// <param name="Name">The protobuf field name.</param>
/// <param name="Type">The protobuf scalar or message type.</param>
/// <param name="Number">The stable protobuf field number.</param>
public sealed record KawaProtoFieldDefinition(string Name, string Type, int Number);

/// <summary>
/// Holds schema-generation output and diagnostics.
/// </summary>
/// <param name="Schema">The generated protobuf schema, or an empty string when diagnostics prevent generation.</param>
/// <param name="Diagnostics">The schema diagnostics that must be reported to the application.</param>
public sealed record KawaProtoSchemaGenerationResult(string Schema, IReadOnlyList<KawaProtoDiagnostic> Diagnostics);
