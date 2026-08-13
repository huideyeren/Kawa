using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kawa.MagicOnion.Generators;

/// <summary>
/// Generates MagicOnion unary and raw-streaming service contracts and adapters for annotated Kawa use cases.
/// </summary>
[Generator]
public sealed class KawaMagicOnionGenerator : IIncrementalGenerator
{
    private const string ServiceAttributeName = "Kawa.MagicOnion.KawaMagicOnionServiceAttribute";
    private const string OperationAttributeName = "Kawa.MagicOnion.KawaMagicOnionOperationAttribute";
    private const string HubAttributeName = "Kawa.MagicOnion.KawaMagicOnionHubAttribute";
    private const string UnaryUseCaseName = "Kawa.Abstractions.IUseCase<TRequest, TResponse>";
    private const string ServerStreamingUseCaseName = "Kawa.Abstractions.IServerStreamingUseCase<TRequest, TResponse>";
    private const string ClientStreamingUseCaseName = "Kawa.Abstractions.IClientStreamingUseCase<TRequest, TResponse>";
    private const string DuplexStreamingUseCaseName = "Kawa.Abstractions.IDuplexStreamingUseCase<TRequest, TResponse>";
    private const string StreamingHubUseCaseName = "Kawa.Abstractions.IStreamingHubUseCase<TClientEvent, TServerEvent>";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var useCases = context.SyntaxProvider.ForAttributeWithMetadataName(
            ServiceAttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, _) => CreateUseCase(attributeContext))
            .Where(static useCase => useCase is not null)
            .Select(static (useCase, _) => useCase!)
            .Collect();

        context.RegisterSourceOutput(useCases, static (productionContext, values) => Emit(productionContext, values));

        var hubs = context.SyntaxProvider.ForAttributeWithMetadataName(
            HubAttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, _) => CreateHub(attributeContext))
            .Where(static hub => hub is not null)
            .Select(static (hub, _) => hub!)
            .Collect();

        context.RegisterSourceOutput(hubs, static (productionContext, values) => EmitHubs(productionContext, values));
    }

    private static UseCaseModel? CreateUseCase(GeneratorAttributeSyntaxContext context)
    {
        var useCaseType = (INamedTypeSymbol)context.TargetSymbol;
        var useCaseContract = useCaseType.AllInterfaces.FirstOrDefault(
            candidate => IsSupportedUseCaseContract(candidate.OriginalDefinition.ToDisplayString()));

        if (useCaseContract is null)
        {
            return null;
        }

        var serviceName = (string?)context.Attributes[0].ConstructorArguments[0].Value;
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return null;
        }

        var operationAttribute = useCaseType.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == OperationAttributeName);
        var operationName = (string?)operationAttribute?.ConstructorArguments.FirstOrDefault().Value;
        if (string.IsNullOrWhiteSpace(operationName))
        {
            operationName = RemoveUseCaseSuffix(useCaseType.Name);
        }

        return new UseCaseModel(
            useCaseType.ContainingNamespace.IsGlobalNamespace ? "Generated" : useCaseType.ContainingNamespace.ToDisplayString() + ".Generated",
            serviceName!,
            operationName!,
            GetUseCaseKind(useCaseContract.OriginalDefinition.ToDisplayString()),
            useCaseContract.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            useCaseContract.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static HubUseCaseModel? CreateHub(GeneratorAttributeSyntaxContext context)
    {
        var useCaseType = (INamedTypeSymbol)context.TargetSymbol;
        var useCaseContract = useCaseType.AllInterfaces.FirstOrDefault(
            candidate => candidate.OriginalDefinition.ToDisplayString() == StreamingHubUseCaseName);

        if (useCaseContract is null)
        {
            return null;
        }

        var hubName = (string?)context.Attributes[0].ConstructorArguments[0].Value;
        if (string.IsNullOrWhiteSpace(hubName))
        {
            return null;
        }

        return new HubUseCaseModel(
            useCaseType.ContainingNamespace.IsGlobalNamespace ? "Generated" : useCaseType.ContainingNamespace.ToDisplayString() + ".Generated",
            hubName!,
            useCaseContract.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            useCaseContract.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static bool IsSupportedUseCaseContract(string contractName)
    {
        return contractName == UnaryUseCaseName
            || contractName == ServerStreamingUseCaseName
            || contractName == ClientStreamingUseCaseName
            || contractName == DuplexStreamingUseCaseName;
    }

    private static UseCaseKind GetUseCaseKind(string contractName)
    {
        return contractName switch
        {
            UnaryUseCaseName => UseCaseKind.Unary,
            ServerStreamingUseCaseName => UseCaseKind.ServerStreaming,
            ClientStreamingUseCaseName => UseCaseKind.ClientStreaming,
            DuplexStreamingUseCaseName => UseCaseKind.DuplexStreaming,
            _ => throw new InvalidOperationException("Unsupported Kawa use case contract."),
        };
    }

    private static string RemoveUseCaseSuffix(string name)
    {
        const string suffix = "UseCase";
        return name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length) : name;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<UseCaseModel> values)
    {
        foreach (var service in values.GroupBy(static value => value.Namespace + "|" + value.ServiceName, StringComparer.Ordinal))
        {
            var first = service.First();
            var serviceIdentifier = ToIdentifier(first.ServiceName);
            var source = new StringBuilder();
            source.AppendLine("// <auto-generated />");
            source.AppendLine("#nullable enable");
            source.Append("namespace ").Append(first.Namespace).AppendLine(";");
            source.AppendLine();
            source.Append("public interface I").Append(serviceIdentifier).AppendLine("MagicOnionService : global::MagicOnion.IService<I" + serviceIdentifier + "MagicOnionService>");
            source.AppendLine("{");

            foreach (var operation in service)
            {
                AppendServiceOperation(source, operation);
            }

            source.AppendLine("}");
            source.AppendLine();
            source.Append("public sealed class ").Append(serviceIdentifier).Append("MagicOnionServiceAdapter : global::MagicOnion.Server.ServiceBase<I")
                .Append(serviceIdentifier).Append("MagicOnionService>, I").Append(serviceIdentifier).AppendLine("MagicOnionService");
            source.AppendLine("{");

            foreach (var operation in service)
            {
                source.Append("    private readonly ").Append(GetUseCaseContractType(operation)).Append(" _")
                    .Append(ToIdentifier(operation.OperationName)).AppendLine("UseCase;");
            }

            source.AppendLine("    private readonly global::Kawa.Core.UseCaseExecutor _executor;");
            source.AppendLine("    private readonly global::Kawa.Core.ServerStreamingUseCaseExecutor _serverStreamingExecutor;");
            source.AppendLine("    private readonly global::Kawa.Core.ClientStreamingUseCaseExecutor _clientStreamingExecutor;");
            source.AppendLine("    private readonly global::Kawa.Core.DuplexStreamingUseCaseExecutor _duplexStreamingExecutor;");
            source.AppendLine();
            source.Append("    public ").Append(serviceIdentifier).Append("MagicOnionServiceAdapter(");
            source.Append(string.Join(", ", service.Select(operation => GetUseCaseContractType(operation) + " " + LowerFirst(ToIdentifier(operation.OperationName)) + "UseCase")));
            source.AppendLine(", global::Kawa.Core.UseCaseExecutor executor, global::Kawa.Core.ServerStreamingUseCaseExecutor serverStreamingExecutor, global::Kawa.Core.ClientStreamingUseCaseExecutor clientStreamingExecutor, global::Kawa.Core.DuplexStreamingUseCaseExecutor duplexStreamingExecutor)");
            source.AppendLine("    {");
            foreach (var operation in service)
            {
                var identifier = ToIdentifier(operation.OperationName);
                source.Append("        _").Append(identifier).Append("UseCase = ").Append(LowerFirst(identifier)).AppendLine("UseCase;");
            }
            source.AppendLine("        _executor = executor;");
            source.AppendLine("        _serverStreamingExecutor = serverStreamingExecutor;");
            source.AppendLine("        _clientStreamingExecutor = clientStreamingExecutor;");
            source.AppendLine("        _duplexStreamingExecutor = duplexStreamingExecutor;");
            source.AppendLine("    }");

            foreach (var operation in service)
            {
                AppendAdapterOperation(source, operation);
            }

            source.AppendLine();
            source.AppendLine("    private static async global::System.Collections.Generic.IAsyncEnumerable<TRequest> ReadClientRequests<TRequest, TResponse>(global::MagicOnion.Server.ClientStreamingContext<TRequest, TResponse> stream, [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken)");
            source.AppendLine("    {");
            source.AppendLine("        while (await stream.MoveNext(cancellationToken))");
            source.AppendLine("        {");
            source.AppendLine("            yield return stream.Current;");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    private static async global::System.Collections.Generic.IAsyncEnumerable<TRequest> ReadDuplexRequests<TRequest, TResponse>(global::MagicOnion.Server.DuplexStreamingContext<TRequest, TResponse> stream, [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken)");
            source.AppendLine("    {");
            source.AppendLine("        while (await stream.MoveNext(cancellationToken))");
            source.AppendLine("        {");
            source.AppendLine("            yield return stream.Current;");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    private void ThrowMappedError(global::Kawa.Abstractions.KawaError error)");
            source.AppendLine("    {");
            source.AppendLine("        var mapped = global::Kawa.MagicOnion.KawaMagicOnionErrorMapper.Map(error);");
            source.AppendLine("        Context.CallContext.ResponseTrailers.Add(\"kawa-error-bin\", mapped.KawaErrorJson);");
            source.AppendLine("        throw new global::MagicOnion.ReturnStatusException(mapped.StatusCode, mapped.Detail);");
            source.AppendLine("    }");

            source.AppendLine("}");
            context.AddSource(serviceIdentifier + ".MagicOnion.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    private static void EmitHubs(SourceProductionContext context, ImmutableArray<HubUseCaseModel> values)
    {
        foreach (var hub in values)
        {
            var hubIdentifier = ToIdentifier(hub.HubName);
            var source = new StringBuilder();
            source.AppendLine("// <auto-generated />");
            source.AppendLine("#nullable enable");
            source.Append("namespace ").Append(hub.Namespace).AppendLine(";");
            source.AppendLine();
            source.Append("public interface I").Append(hubIdentifier).AppendLine("MagicOnionHubReceiver");
            source.AppendLine("{");
            source.Append("    void OnEvent(").Append(hub.ServerEventType).AppendLine(" serverEvent);");
            source.AppendLine("}");
            source.AppendLine();
            source.Append("public interface I").Append(hubIdentifier).Append("MagicOnionHub : global::MagicOnion.IStreamingHub<I")
                .Append(hubIdentifier).Append("MagicOnionHub, I").Append(hubIdentifier).AppendLine("MagicOnionHubReceiver>");
            source.AppendLine("{");
            source.Append("    global::System.Threading.Tasks.ValueTask HandleAsync(").Append(hub.ClientEventType).AppendLine(" clientEvent);");
            source.AppendLine("}");
            source.AppendLine();
            source.Append("public sealed class ").Append(hubIdentifier).Append("MagicOnionHubAdapter : global::MagicOnion.Server.Hubs.StreamingHubBase<I")
                .Append(hubIdentifier).Append("MagicOnionHub, I").Append(hubIdentifier).Append("MagicOnionHubReceiver>, I").Append(hubIdentifier).AppendLine("MagicOnionHub");
            source.AppendLine("{");
            source.Append("    private readonly global::Kawa.Abstractions.IStreamingHubUseCase<").Append(hub.ClientEventType).Append(", ").Append(hub.ServerEventType).AppendLine("> _useCase;");
            source.AppendLine("    private readonly global::Kawa.Core.StreamingHubUseCaseExecutor _executor;");
            source.Append("    private readonly global::System.Collections.Generic.Dictionary<string, global::MagicOnion.Server.Hubs.IGroup<I").Append(hubIdentifier).AppendLine("MagicOnionHubReceiver>> _groups = new global::System.Collections.Generic.Dictionary<string, global::MagicOnion.Server.Hubs.IGroup<I" + hubIdentifier + "MagicOnionHubReceiver>>();");
            source.Append("    private HubSession? _session;").AppendLine();
            source.AppendLine();
            source.Append("    public ").Append(hubIdentifier).Append("MagicOnionHubAdapter(global::Kawa.Abstractions.IStreamingHubUseCase<").Append(hub.ClientEventType).Append(", ").Append(hub.ServerEventType).AppendLine("> useCase, global::Kawa.Core.StreamingHubUseCaseExecutor executor)");
            source.AppendLine("    {");
            source.AppendLine("        _useCase = useCase;");
            source.AppendLine("        _executor = executor;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    protected override async global::System.Threading.Tasks.ValueTask OnConnected()");
            source.AppendLine("    {");
            source.AppendLine("        _session = new HubSession(this, ConnectionId.ToString());");
            source.AppendLine("        await _executor.OnConnectedAsync(_useCase, _session, Context.CallContext.CancellationToken);");
            source.AppendLine("    }");
            source.AppendLine();
            source.Append("    public async global::System.Threading.Tasks.ValueTask HandleAsync(").Append(hub.ClientEventType).AppendLine(" clientEvent)");
            source.AppendLine("    {");
            source.AppendLine("        if (_session is null) throw new global::System.InvalidOperationException(\"The hub session is not connected.\");");
            source.AppendLine("        await _executor.HandleAsync(_useCase, clientEvent, _session, Context.CallContext.CancellationToken);");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    protected override async global::System.Threading.Tasks.ValueTask OnDisconnected()");
            source.AppendLine("    {");
            source.AppendLine("        if (_session is not null) await _executor.OnDisconnectedAsync(_useCase, _session, null, Context.CallContext.CancellationToken);");
            source.AppendLine("    }");
            source.AppendLine();
            source.Append("    private global::System.Threading.Tasks.Task SendAsync(").Append(hub.ServerEventType).AppendLine(" serverEvent)");
            source.AppendLine("    {");
            source.AppendLine("        Client.OnEvent(serverEvent);");
            source.AppendLine("        return global::System.Threading.Tasks.Task.CompletedTask;");
            source.AppendLine("    }");
            source.AppendLine();
            source.Append("    private async global::System.Threading.Tasks.Task JoinGroupAsync(string groupName)").AppendLine();
            source.AppendLine("    {");
            source.AppendLine("        _groups[groupName] = await Group.AddAsync(groupName);");
            source.AppendLine("    }");
            source.AppendLine();
            source.Append("    private async global::System.Threading.Tasks.Task LeaveGroupAsync(string groupName)").AppendLine();
            source.AppendLine("    {");
            source.AppendLine("        if (_groups.TryGetValue(groupName, out var group))");
            source.AppendLine("        {");
            source.AppendLine("            _groups.Remove(groupName);");
            source.AppendLine("            await group.RemoveAsync(Context);");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine();
            source.Append("    private global::System.Threading.Tasks.Task BroadcastToGroupAsync(string groupName, ").Append(hub.ServerEventType).AppendLine(" serverEvent)");
            source.AppendLine("    {");
            source.AppendLine("        if (!_groups.TryGetValue(groupName, out var group)) throw new global::System.InvalidOperationException(\"The hub session has not joined the group.\");");
            source.AppendLine("        group.All.OnEvent(serverEvent);");
            source.AppendLine("        return global::System.Threading.Tasks.Task.CompletedTask;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    private sealed class HubSession : global::Kawa.Abstractions.IKawaHubSession<" + hub.ServerEventType + ">");
            source.AppendLine("    {");
            source.Append("        private readonly ").Append(hubIdentifier).AppendLine("MagicOnionHubAdapter _hub;");
            source.AppendLine("        public HubSession(" + hubIdentifier + "MagicOnionHubAdapter hub, string connectionId) { _hub = hub; ConnectionId = connectionId; }");
            source.AppendLine("        public string ConnectionId { get; }");
            source.Append("        public global::System.Threading.Tasks.Task SendAsync(").Append(hub.ServerEventType).AppendLine(" serverEvent, global::System.Threading.CancellationToken cancellationToken = default) => _hub.SendAsync(serverEvent);");
            source.Append("        public global::System.Threading.Tasks.Task BroadcastToGroupAsync(string groupName, ").Append(hub.ServerEventType).AppendLine(" serverEvent, global::System.Threading.CancellationToken cancellationToken = default) => _hub.BroadcastToGroupAsync(groupName, serverEvent);");
            source.AppendLine("        public global::System.Threading.Tasks.Task JoinGroupAsync(string groupName, global::System.Threading.CancellationToken cancellationToken = default) => _hub.JoinGroupAsync(groupName);");
            source.AppendLine("        public global::System.Threading.Tasks.Task LeaveGroupAsync(string groupName, global::System.Threading.CancellationToken cancellationToken = default) => _hub.LeaveGroupAsync(groupName);");
            source.AppendLine("    }");
            source.AppendLine("}");
            context.AddSource(hubIdentifier + ".MagicOnionHub.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    private static void AppendServiceOperation(StringBuilder source, UseCaseModel operation)
    {
        var identifier = ToIdentifier(operation.OperationName);
        switch (operation.Kind)
        {
            case UseCaseKind.Unary:
                source.Append("    global::MagicOnion.UnaryResult<").Append(operation.ResponseType).Append("> ").Append(identifier).Append("Async(").Append(operation.RequestType).AppendLine(" request);");
                break;
            case UseCaseKind.ServerStreaming:
                source.Append("    global::System.Threading.Tasks.Task<global::MagicOnion.ServerStreamingResult<").Append(operation.ResponseType).Append(">> ").Append(identifier).Append("Async(").Append(operation.RequestType).AppendLine(" request);");
                break;
            case UseCaseKind.ClientStreaming:
                source.Append("    global::System.Threading.Tasks.Task<global::MagicOnion.ClientStreamingResult<").Append(operation.RequestType).Append(", ").Append(operation.ResponseType).Append(">> ").Append(identifier).AppendLine("Async();");
                break;
            case UseCaseKind.DuplexStreaming:
                source.Append("    global::System.Threading.Tasks.Task<global::MagicOnion.DuplexStreamingResult<").Append(operation.RequestType).Append(", ").Append(operation.ResponseType).Append(">> ").Append(identifier).AppendLine("Async();");
                break;
        }
    }

    private static void AppendAdapterOperation(StringBuilder source, UseCaseModel operation)
    {
        var identifier = ToIdentifier(operation.OperationName);
        source.AppendLine();
        switch (operation.Kind)
        {
            case UseCaseKind.Unary:
                source.Append("    public async global::MagicOnion.UnaryResult<").Append(operation.ResponseType).Append("> ").Append(identifier).Append("Async(").Append(operation.RequestType).AppendLine(" request)");
                source.AppendLine("    {");
                source.Append("        var result = await _executor.ExecuteAsync(_").Append(identifier).AppendLine("UseCase, request, Context.CallContext.CancellationToken);");
                source.AppendLine("        if (result.IsSuccess) return result.Value!;");
                source.AppendLine("        ThrowMappedError(result.Error!);");
                source.AppendLine("        throw new global::System.InvalidOperationException();");
                source.AppendLine("    }");
                break;
            case UseCaseKind.ServerStreaming:
                source.Append("    public async global::System.Threading.Tasks.Task<global::MagicOnion.ServerStreamingResult<").Append(operation.ResponseType).Append(">> ").Append(identifier).Append("Async(").Append(operation.RequestType).AppendLine(" request)");
                source.AppendLine("    {");
                source.Append("        var stream = GetServerStreamingContext<").Append(operation.ResponseType).AppendLine(">();");
                source.Append("        await foreach (var result in _serverStreamingExecutor.ExecuteAsync(_").Append(identifier).AppendLine("UseCase, request, Context.CallContext.CancellationToken))");
                source.AppendLine("        {");
                source.AppendLine("            if (result.IsFailure) ThrowMappedError(result.Error!);");
                source.AppendLine("            await stream.WriteAsync(result.Value!);");
                source.AppendLine("        }");
                source.AppendLine("        return stream.Result();");
                source.AppendLine("    }");
                break;
            case UseCaseKind.ClientStreaming:
                source.Append("    public async global::System.Threading.Tasks.Task<global::MagicOnion.ClientStreamingResult<").Append(operation.RequestType).Append(", ").Append(operation.ResponseType).Append(">> ").Append(identifier).AppendLine("Async()");
                source.AppendLine("    {");
                source.Append("        var stream = GetClientStreamingContext<").Append(operation.RequestType).Append(", ").Append(operation.ResponseType).AppendLine(">();");
                source.Append("        var result = await _clientStreamingExecutor.ExecuteAsync(_").Append(identifier).AppendLine("UseCase, ReadClientRequests(stream, Context.CallContext.CancellationToken), Context.CallContext.CancellationToken);");
                source.AppendLine("        if (result.IsFailure) ThrowMappedError(result.Error!);");
                source.AppendLine("        return stream.Result(result.Value!);");
                source.AppendLine("    }");
                break;
            case UseCaseKind.DuplexStreaming:
                source.Append("    public async global::System.Threading.Tasks.Task<global::MagicOnion.DuplexStreamingResult<").Append(operation.RequestType).Append(", ").Append(operation.ResponseType).Append(">> ").Append(identifier).AppendLine("Async()");
                source.AppendLine("    {");
                source.Append("        var stream = GetDuplexStreamingContext<").Append(operation.RequestType).Append(", ").Append(operation.ResponseType).AppendLine(">();");
                source.Append("        await foreach (var result in _duplexStreamingExecutor.ExecuteAsync(_").Append(identifier).AppendLine("UseCase, ReadDuplexRequests(stream, Context.CallContext.CancellationToken), Context.CallContext.CancellationToken))");
                source.AppendLine("        {");
                source.AppendLine("            if (result.IsFailure) ThrowMappedError(result.Error!);");
                source.AppendLine("            await stream.WriteAsync(result.Value!);");
                source.AppendLine("        }");
                source.AppendLine("        return stream.Result();");
                source.AppendLine("    }");
                break;
        }
    }

    private static string GetUseCaseContractType(UseCaseModel operation)
    {
        return operation.Kind switch
        {
            UseCaseKind.Unary => "global::Kawa.Abstractions.IUseCase<" + operation.RequestType + ", " + operation.ResponseType + ">",
            UseCaseKind.ServerStreaming => "global::Kawa.Abstractions.IServerStreamingUseCase<" + operation.RequestType + ", " + operation.ResponseType + ">",
            UseCaseKind.ClientStreaming => "global::Kawa.Abstractions.IClientStreamingUseCase<" + operation.RequestType + ", " + operation.ResponseType + ">",
            UseCaseKind.DuplexStreaming => "global::Kawa.Abstractions.IDuplexStreamingUseCase<" + operation.RequestType + ", " + operation.ResponseType + ">",
            _ => throw new InvalidOperationException("Unsupported Kawa use case contract."),
        };
    }

    private static string ToIdentifier(string value)
    {
        var parts = value.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(static part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
    }

    private static string LowerFirst(string value) => char.ToLowerInvariant(value[0]) + value.Substring(1);

    private enum UseCaseKind
    {
        Unary,
        ServerStreaming,
        ClientStreaming,
        DuplexStreaming,
    }

    private sealed class UseCaseModel
    {
        public UseCaseModel(string @namespace, string serviceName, string operationName, UseCaseKind kind, string requestType, string responseType)
        {
            Namespace = @namespace;
            ServiceName = serviceName;
            OperationName = operationName;
            Kind = kind;
            RequestType = requestType;
            ResponseType = responseType;
        }

        public string Namespace { get; }
        public string ServiceName { get; }
        public string OperationName { get; }
        public UseCaseKind Kind { get; }
        public string RequestType { get; }
        public string ResponseType { get; }
    }

    private sealed class HubUseCaseModel
    {
        public HubUseCaseModel(string @namespace, string hubName, string clientEventType, string serverEventType)
        {
            Namespace = @namespace;
            HubName = hubName;
            ClientEventType = clientEventType;
            ServerEventType = serverEventType;
        }

        public string Namespace { get; }
        public string HubName { get; }
        public string ClientEventType { get; }
        public string ServerEventType { get; }
    }
}
