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
        .Append(MetadataReference.CreateFromFile(typeof(Kawa.MagicOnion.KawaMagicOnionServiceAttribute).Assembly.Location))
        .ToArray();
}
