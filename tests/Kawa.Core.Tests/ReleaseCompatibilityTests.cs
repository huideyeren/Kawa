using System.Text.Json;
using Kawa.Abstractions;

namespace Kawa.Core.Tests;

/// <summary>Protects existing serialized contracts while adding 0.5 transport metadata.</summary>
public sealed class ReleaseCompatibilityTests
{
    /// <summary>Existing numeric error values are a compatibility contract.</summary>
    [Theory]
    [InlineData(KawaErrorKind.Validation, 0)]
    [InlineData(KawaErrorKind.NotFound, 1)]
    [InlineData(KawaErrorKind.Unauthorized, 2)]
    [InlineData(KawaErrorKind.Forbidden, 3)]
    [InlineData(KawaErrorKind.Conflict, 4)]
    [InlineData(KawaErrorKind.Unknown, 5)]
    public void ErrorKind_PreservesExistingNumericValues(KawaErrorKind kind, int expected) =>
        Assert.Equal(expected, (int)kind);

    /// <summary>Catalog extensions are absent when no transport metadata was supplied.</summary>
    [Fact]
    public void Catalog_OmitsAbsentTransportsAndPreservesLegacyFields()
    {
        var entry = KawaUseCaseCatalog.FromUseCaseType(typeof(Send));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(KawaApiCatalog.FromEntries([entry]),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var item = json.RootElement.GetProperty("useCases")[0];
        Assert.Equal("Send", item.GetProperty("name").GetString());
        Assert.Equal("v1", item.GetProperty("version").GetString());
        Assert.False(item.TryGetProperty("transports", out _));
        Assert.True(item.TryGetProperty("request", out _));
        Assert.True(item.TryGetProperty("response", out _));
        Assert.True(item.TryGetProperty("errorResponses", out _));
    }

    /// <summary>Serializes explicit bindings without changing existing fields.</summary>
    [Fact]
    public void Catalog_SerializesExplicitTransportMetadata()
    {
        var entry = KawaUseCaseCatalog.FromUseCaseType(typeof(Send)) with
        {
            Transports = [new("http"), new("grpc")],
        };
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(KawaApiCatalog.FromEntries([entry]),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var transports = json.RootElement.GetProperty("useCases")[0].GetProperty("transports");
        Assert.Equal("grpc", transports[0].GetProperty("kind").GetString());
        Assert.Equal("http", transports[1].GetProperty("kind").GetString());
    }

    /// <summary>Merges duplicate bindings into one use-case entry.</summary>
    [Fact]
    public void Catalog_MergesBindingsForSameEntry()
    {
        var entry = KawaUseCaseCatalog.FromUseCaseType(typeof(Send));
        var catalog = KawaApiCatalog.FromEntries([
            entry with { Transports = [new("http")] },
            entry with { Transports = [new("grpc"), new("http")] },
        ]);
        Assert.Equal(2, Assert.Single(catalog.UseCases).Transports!.Count);
    }

    private sealed class Send : IUseCase<string, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
            Task.FromResult(KawaResult<string>.Success(request));
    }
}
