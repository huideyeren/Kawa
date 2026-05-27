using Kawa.Abstractions;

namespace Kawa.Core.Tests;

/// <summary>
/// Verifies the public API catalog model.
/// </summary>
public sealed class KawaApiCatalogTests
{
    /// <summary>
    /// Verifies that catalog entries are converted into a stable public catalog.
    /// </summary>
    [Fact]
    public void FromEntries_ConvertsEntriesToPublicCatalogAndSortsByName()
    {
        var entries = new[]
        {
            CreateEntry("users.update", typeof(UpdateUserRequest), typeof(UpdateUserResponse), KawaErrorKind.Conflict),
            CreateEntry("users.create", typeof(CreateUserRequest), typeof(CreateUserResponse), KawaErrorKind.Validation),
        };

        var catalog = KawaApiCatalog.FromEntries(entries);

        Assert.Collection(
            catalog.UseCases,
            useCase =>
            {
                Assert.Equal("users.create", useCase.Name);
                Assert.Equal("Create users.create", useCase.Summary);
                Assert.Equal("Runs users.create.", useCase.Description);
                Assert.Equal("v1", useCase.Version);
                Assert.Equal(new[] { "users", "test" }, useCase.Tags);
                Assert.Equal(nameof(CreateUserRequest), useCase.Request.Name);
                Assert.Equal(typeof(CreateUserRequest).FullName, useCase.Request.FullName);
                Assert.Equal(nameof(CreateUserResponse), useCase.Response.Name);
                Assert.Equal(typeof(CreateUserResponse).FullName, useCase.Response.FullName);
                var error = Assert.Single(useCase.ErrorResponses);
                Assert.Equal("Validation", error.Kind);
                Assert.Equal("Validation failed.", error.Description);
            },
            useCase =>
            {
                Assert.Equal("users.update", useCase.Name);
                Assert.Equal(nameof(UpdateUserRequest), useCase.Request.Name);
                Assert.Equal(nameof(UpdateUserResponse), useCase.Response.Name);
                var error = Assert.Single(useCase.ErrorResponses);
                Assert.Equal("Conflict", error.Kind);
            });
    }

    /// <summary>
    /// Verifies that null catalog entries are rejected.
    /// </summary>
    [Fact]
    public void FromEntries_ThrowsForNullEntries()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => KawaApiCatalog.FromEntries(null!));

        Assert.Equal("entries", exception.ParamName);
    }

    /// <summary>
    /// Verifies that null contract types are rejected.
    /// </summary>
    [Fact]
    public void FromType_ThrowsForNullType()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => KawaApiCatalogContract.FromType(null!));

        Assert.Equal("type", exception.ParamName);
    }

    /// <summary>
    /// Verifies that null error response metadata is rejected.
    /// </summary>
    [Fact]
    public void FromMetadata_ThrowsForNullMetadata()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => KawaApiCatalogErrorResponse.FromMetadata(null!));

        Assert.Equal("metadata", exception.ParamName);
    }

    /// <summary>
    /// Verifies that null use case catalog entries are rejected.
    /// </summary>
    [Fact]
    public void FromEntry_ThrowsForNullEntry()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => KawaApiCatalogUseCase.FromEntry(null!));

        Assert.Equal("entry", exception.ParamName);
    }

    private static KawaUseCaseCatalogEntry CreateEntry(
        string name,
        Type requestType,
        Type responseType,
        KawaErrorKind errorKind)
    {
        return new KawaUseCaseCatalogEntry(
            typeof(KawaApiCatalogTests),
            requestType,
            responseType,
            new KawaUseCaseMetadata(
                name,
                $"Create {name}",
                $"Runs {name}.",
                "v1",
                new[] { "users", "test" }),
            new[]
            {
                new KawaErrorResponseMetadata(errorKind, $"{errorKind} failed."),
            });
    }

    private sealed record CreateUserRequest(string Name);

    private sealed record CreateUserResponse(string Id);

    private sealed record UpdateUserRequest(string Id, string Name);

    private sealed record UpdateUserResponse(string Id);
}
