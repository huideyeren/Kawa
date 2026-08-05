using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Kawa.MagicOnion.Generators.Tests;

/// <summary>
/// Verifies MagicOnion source generation for Kawa unary use cases.
/// </summary>
public sealed class KawaMagicOnionGeneratorTests
{
    /// <summary>
    /// Verifies that a typed Kawa hub use case produces both the MagicOnion hub
    /// interface and its receiver interface.
    /// </summary>
    [Fact]
    public void Generator_EmitsStreamingHubAndReceiverInterfaces()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Kawa.Abstractions;
            using Kawa.MagicOnion;
            namespace Demo;
            [KawaMagicOnionService("chat-service")]
            public sealed class PingUseCase : IUseCase<string, int>
            {
                public Task<KawaResult<int>> ExecuteAsync(string request, CancellationToken cancellationToken = default) => throw null!;
            }
            [KawaMagicOnionHub("chat")]
            public sealed class ChatUseCase : IStreamingHubUseCase<string, int>
            {
                public Task OnConnectedAsync(IKawaHubSession<int> session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task HandleAsync(string clientEvent, IKawaHubSession<int> session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task OnDisconnectedAsync(IKawaHubSession<int> session, System.Exception? exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Demo",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new KawaMagicOnionGenerator());

        driver = driver.RunGenerators(compilation);
        var generated = string.Concat(driver.GetRunResult().GeneratedTrees.Select(static tree => tree.GetText().ToString()));

        Assert.Contains("interface IChatMagicOnionHub : global::MagicOnion.IStreamingHub<IChatMagicOnionHub, IChatMagicOnionHubReceiver>", generated);
        Assert.Contains("global::System.Threading.Tasks.ValueTask HandleAsync(string clientEvent)", generated);
        Assert.Contains("interface IChatMagicOnionHubReceiver", generated);
        Assert.Contains("void OnEvent(int serverEvent)", generated);
    }

    /// <summary>
    /// Verifies that a generated hub delegates its lifecycle and typed client events
    /// through Kawa's transport-neutral hub executor.
    /// </summary>
    [Fact]
    public void Generator_EmitsStreamingHubAdapter()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Kawa.Abstractions;
            using Kawa.MagicOnion;
            namespace Demo;
            [KawaMagicOnionHub("chat")]
            public sealed class ChatUseCase : IStreamingHubUseCase<string, int>
            {
                public Task OnConnectedAsync(IKawaHubSession<int> session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task HandleAsync(string clientEvent, IKawaHubSession<int> session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task OnDisconnectedAsync(IKawaHubSession<int> session, System.Exception? exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Demo",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new KawaMagicOnionGenerator());

        driver = driver.RunGenerators(compilation);
        var generated = string.Concat(driver.GetRunResult().GeneratedTrees.Select(static tree => tree.GetText().ToString()));

        Assert.Contains("sealed class ChatMagicOnionHubAdapter : global::MagicOnion.Server.Hubs.StreamingHubBase<IChatMagicOnionHub, IChatMagicOnionHubReceiver>, IChatMagicOnionHub", generated);
        Assert.Contains("override async global::System.Threading.Tasks.ValueTask OnConnected()", generated);
        Assert.Contains("_executor.HandleAsync(_useCase, clientEvent, _session, Context.CallContext.CancellationToken)", generated);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var generatedCompilation, out _);
        Assert.Empty(generatedCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Verifies that all transport-neutral streaming use case contracts become the
    /// corresponding MagicOnion raw-streaming methods on one generated service.
    /// </summary>
    [Fact]
    public void Generator_EmitsAllRawStreamingServiceMethods()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Kawa.Abstractions;
            using Kawa.MagicOnion;
            namespace Demo;
            [KawaMagicOnionService("chat")]
            [KawaMagicOnionOperation("send")]
            public sealed class SendUseCase : IUseCase<string, int>
            {
                public Task<KawaResult<int>> ExecuteAsync(string request, CancellationToken cancellationToken = default) => throw null!;
            }
            [KawaMagicOnionService("chat")]
            [KawaMagicOnionOperation("watch")]
            public sealed class WatchUseCase : IServerStreamingUseCase<string, int>
            {
                public async IAsyncEnumerable<KawaResult<int>> ExecuteAsync(string request, CancellationToken cancellationToken = default) { yield break; }
            }
            [KawaMagicOnionService("chat")]
            [KawaMagicOnionOperation("upload")]
            public sealed class UploadUseCase : IClientStreamingUseCase<string, int>
            {
                public Task<KawaResult<int>> ExecuteAsync(IAsyncEnumerable<string> requests, CancellationToken cancellationToken = default) => throw null!;
            }
            [KawaMagicOnionService("chat")]
            [KawaMagicOnionOperation("exchange")]
            public sealed class ExchangeUseCase : IDuplexStreamingUseCase<string, int>
            {
                public async IAsyncEnumerable<KawaResult<int>> ExecuteAsync(IAsyncEnumerable<string> requests, CancellationToken cancellationToken = default) { yield break; }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Demo",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new KawaMagicOnionGenerator());

        driver = driver.RunGenerators(compilation);
        var generated = driver.GetRunResult().GeneratedTrees.Single().GetText().ToString();

        Assert.Contains("Task<global::MagicOnion.ServerStreamingResult<int>> WatchAsync(string request)", generated);
        Assert.Contains("Task<global::MagicOnion.ClientStreamingResult<string, int>> UploadAsync()", generated);
        Assert.Contains("Task<global::MagicOnion.DuplexStreamingResult<string, int>> ExchangeAsync()", generated);
    }

    /// <summary>
    /// Verifies that raw-streaming adapters compile against the MagicOnion server
    /// runtime rather than merely matching a generated-source snapshot.
    /// </summary>
    [Fact]
    public void Generator_ProducesCompilableRawStreamingAdapters()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Kawa.Abstractions;
            using Kawa.MagicOnion;
            namespace Demo;
            [KawaMagicOnionService("chat")]
            public sealed class WatchUseCase : IServerStreamingUseCase<string, int>
            {
                public async IAsyncEnumerable<KawaResult<int>> ExecuteAsync(string request, CancellationToken cancellationToken = default) { yield break; }
            }
            [KawaMagicOnionService("chat")]
            public sealed class UploadUseCase : IClientStreamingUseCase<string, int>
            {
                public Task<KawaResult<int>> ExecuteAsync(IAsyncEnumerable<string> requests, CancellationToken cancellationToken = default) => throw null!;
            }
            [KawaMagicOnionService("chat")]
            public sealed class ExchangeUseCase : IDuplexStreamingUseCase<string, int>
            {
                public async IAsyncEnumerable<KawaResult<int>> ExecuteAsync(IAsyncEnumerable<string> requests, CancellationToken cancellationToken = default) { yield break; }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Demo",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new KawaMagicOnionGenerator());

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var generatedCompilation, out _);
        var errors = generatedCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.Empty(errors);
    }

    /// <summary>
    /// Verifies that a named unary use case produces a shared service contract and adapter.
    /// </summary>
    [Fact]
    public void Generator_EmitsUnaryServiceAndAdapter()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Kawa.Abstractions;
            using Kawa.MagicOnion;
            namespace Demo;
            [KawaMagicOnionService("chat")]
            [KawaMagicOnionOperation("send")]
            public sealed class SendUseCase : IUseCase<string, int>
            {
                public Task<KawaResult<int>> ExecuteAsync(string request, CancellationToken cancellationToken = default) => throw null!;
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Demo",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new KawaMagicOnionGenerator());

        driver = driver.RunGenerators(compilation);
        var generated = driver.GetRunResult().GeneratedTrees.Single().GetText().ToString();

        Assert.Contains("interface IChatMagicOnionService", generated);
        Assert.Contains("UnaryResult<int> SendAsync(string request)", generated);
        Assert.Contains("sealed class ChatMagicOnionServiceAdapter", generated);
    }

    private static IReadOnlyList<MetadataReference> References { get; } =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(Kawa.Abstractions.IUseCase<,>).Assembly.Location))
        .Append(MetadataReference.CreateFromFile(typeof(Kawa.Core.UseCaseExecutor).Assembly.Location))
        .Append(MetadataReference.CreateFromFile(typeof(Kawa.MagicOnion.KawaMagicOnionServiceAttribute).Assembly.Location))
        .Append(MetadataReference.CreateFromFile(typeof(global::MagicOnion.IService<>).Assembly.Location))
        .Append(MetadataReference.CreateFromFile(typeof(global::MagicOnion.Server.ServiceBase<>).Assembly.Location))
        .ToArray();
}
