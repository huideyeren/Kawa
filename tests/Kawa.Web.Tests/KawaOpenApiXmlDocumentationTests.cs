using System.Reflection;
using System.Reflection.Emit;
using Kawa.Web;

namespace Kawa.Web.Tests;

/// <summary>
/// Verifies XML documentation indexing used by Kawa OpenAPI schemas.
/// </summary>
public sealed class KawaOpenApiXmlDocumentationTests
{
    /// <summary>
    /// Verifies that type and property summaries are indexed and whitespace is normalized.
    /// </summary>
    [Fact]
    public void Load_IndexesTypeAndPropertySummaries()
    {
        var documentationPath = GetTemporaryDocumentationPath();
        var typeName = KawaXmlDocumentationIndex.GetDocumentationTypeName(typeof(DocumentedContract));
        var property = typeof(DocumentedContract).GetProperty(nameof(DocumentedContract.Name))!;

        try
        {
            File.WriteAllText(
                documentationPath,
                $$"""
                <doc>
                  <members>
                    <member name="T:{{typeName}}">
                      <summary>
                        Represents a documented
                        contract.
                      </summary>
                    </member>
                    <member name="P:{{typeName}}.Name">
                      <summary>Gets the contract name.</summary>
                    </member>
                    <member>
                      <summary>This member has no documentation ID.</summary>
                    </member>
                    <member name="P:{{typeName}}.Undocumented" />
                  </members>
                </doc>
                """);

            var index = KawaXmlDocumentationIndex.Load(documentationPath);

            Assert.Equal("Represents a documented contract.", index.GetTypeSummary(typeof(DocumentedContract)));
            Assert.Equal("Gets the contract name.", index.GetPropertySummary(property));
        }
        finally
        {
            File.Delete(documentationPath);
        }
    }

    /// <summary>
    /// Verifies that absent and malformed optional documentation files produce an empty index.
    /// </summary>
    [Fact]
    public void Load_ReturnsEmptyIndexWhenDocumentationIsUnavailable()
    {
        var documentationPath = GetTemporaryDocumentationPath();

        Assert.Same(KawaXmlDocumentationIndex.Empty, KawaXmlDocumentationIndex.Load((string?)null));
        Assert.Same(KawaXmlDocumentationIndex.Empty, KawaXmlDocumentationIndex.Load(documentationPath));

        try
        {
            File.WriteAllText(documentationPath, "<doc />");
            Assert.NotSame(KawaXmlDocumentationIndex.Empty, KawaXmlDocumentationIndex.Load(documentationPath));

            File.WriteAllText(documentationPath, "<doc>");

            Assert.Same(KawaXmlDocumentationIndex.Empty, KawaXmlDocumentationIndex.Load(documentationPath));
        }
        finally
        {
            File.Delete(documentationPath);
        }
    }

    /// <summary>
    /// Verifies that assemblies without a physical location do not require documentation files.
    /// </summary>
    [Fact]
    public void Load_ReturnsEmptyIndexForDynamicAssembly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"Kawa.Dynamic.{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);

        Assert.Same(KawaXmlDocumentationIndex.Empty, KawaXmlDocumentationIndex.Load(assembly));
    }

    /// <summary>
    /// Verifies that constructed generic types use the XML documentation ID of their definition.
    /// </summary>
    [Fact]
    public void GetDocumentationTypeName_UsesGenericTypeDefinition()
    {
        Assert.Equal(
            typeof(GenericContract<>).FullName!.Replace('+', '.'),
            KawaXmlDocumentationIndex.GetDocumentationTypeName(typeof(GenericContract<string>)));
    }

    /// <summary>
    /// Verifies summary normalization behavior.
    /// </summary>
    /// <param name="summary">The raw XML summary.</param>
    /// <param name="expected">The normalized OpenAPI description.</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  first\n second  ", "first second")]
    public void NormalizeSummary_ReturnsExpectedDescription(string? summary, string? expected)
    {
        Assert.Equal(expected, KawaXmlDocumentationIndex.NormalizeSummary(summary));
    }

    private static string GetTemporaryDocumentationPath()
    {
        return Path.Combine(Path.GetTempPath(), $"kawa-{Guid.NewGuid():N}.xml");
    }

    private sealed class DocumentedContract
    {
        public required string Name { get; init; }
    }

    private sealed class GenericContract<T>
    {
    }
}
