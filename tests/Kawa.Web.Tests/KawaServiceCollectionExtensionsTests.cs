using Kawa.Core;
using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Web.Tests;

/// <summary>
/// Verifies Kawa dependency injection registration.
/// </summary>
public sealed class KawaServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that AddKawa registers the core services needed by Kawa endpoints.
    /// </summary>
    [Fact]
    public void AddKawa_RegistersUseCaseExecutor()
    {
        var services = new ServiceCollection();

        services.AddKawa();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<UseCaseExecutor>());
    }

    /// <summary>
    /// Verifies that AddKawa registers HTTP transport mappers.
    /// </summary>
    [Fact]
    public void AddKawa_RegistersHttpTransportMappers()
    {
        var services = new ServiceCollection();

        services.AddKawa();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IResultMapper<IResult>>());
        Assert.NotNull(serviceProvider.GetService<IErrorMapper<IResult>>());
        Assert.NotNull(serviceProvider.GetService<ITransportMapper<IResult>>());
    }

    /// <summary>
    /// Verifies that convention-based registration wires use case implementations by their request/response contract.
    /// </summary>
    [Fact]
    public void AddKawaUseCasesFromAssemblies_RegistersUseCaseContracts()
    {
        var services = new ServiceCollection();

        services.AddKawaUseCasesFromAssemblies(typeof(ConventionalCreateUser).Assembly);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.IsType<ConventionalCreateUser>(
            serviceProvider.GetRequiredService<IUseCase<ConventionalCreateUser.Request, ConventionalCreateUser.Response>>());
    }

    /// <summary>
    /// Verifies that convention-based registration exposes use case catalog entries.
    /// </summary>
    [Fact]
    public void AddKawaUseCasesFromAssemblies_RegistersUseCaseCatalogEntries()
    {
        var services = new ServiceCollection();

        services.AddKawaUseCasesFromAssemblies(typeof(ConventionalCreateUser).Assembly);

        using var serviceProvider = services.BuildServiceProvider();
        var catalogEntry = Assert.Single(
            serviceProvider.GetServices<KawaUseCaseCatalogEntry>(),
            entry => entry.UseCaseType == typeof(ConventionalCreateUser));
        Assert.Equal(typeof(ConventionalCreateUser.Request), catalogEntry.RequestType);
        Assert.Equal(typeof(ConventionalCreateUser.Response), catalogEntry.ResponseType);
        Assert.Contains(catalogEntry.ErrorResponses, response => response.Kind == KawaErrorKind.Validation);
    }

    private sealed class ConventionalCreateUser
        : IUseCase<ConventionalCreateUser.Request, ConventionalCreateUser.Response>
    {
        public sealed record Request(string Name);

        public sealed record Response(string Message);

        public Task<KawaResult<Response>> ExecuteAsync(
            Request request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KawaResult<Response>.Success(new Response(request.Name)));
        }
    }
}
