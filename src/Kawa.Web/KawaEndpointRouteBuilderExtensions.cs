using Kawa.Abstractions;
using Kawa.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa use cases to ASP.NET Core endpoints.
/// </summary>
public static class KawaEndpointRouteBuilderExtensions
{
    private static readonly MethodInfo MapKawaPostFromContractMethod =
        typeof(KawaEndpointRouteBuilderExtensions)
            .GetMethod(nameof(MapKawaPostFromContract), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find the Kawa POST mapping helper.");

    /// <summary>
    /// Maps an HTTP POST endpoint that executes a Kawa use case resolved from dependency injection.
    /// </summary>
    /// <typeparam name="TRequest">The request body contract type.</typeparam>
    /// <typeparam name="TResponse">The successful response contract type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern to map.</param>
    /// <returns>The mapped route handler builder.</returns>
    public static RouteHandlerBuilder MapKawaPost<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPost(
            pattern,
            async (
                [FromBody] TRequest request,
                IServiceProvider services,
                CancellationToken cancellationToken) =>
            {
                var useCase = services.GetRequiredService<IUseCase<TRequest, TResponse>>();
                var executor = services.GetRequiredService<UseCaseExecutor>();
                var mapper = services.GetRequiredService<ITransportMapper<IResult>>();
                var result = await executor.ExecuteAsync(useCase, request, cancellationToken);

                return mapper.Map(result);
            })
            .WithKawaPostOpenApiMetadata<TRequest, TResponse>();
    }

    /// <summary>
    /// Maps an HTTP POST endpoint for a use case type that implements exactly one request/response contract.
    /// </summary>
    /// <typeparam name="TUseCase">The use case implementation type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern to map.</param>
    /// <returns>The mapped route handler builder.</returns>
    public static RouteHandlerBuilder MapKawaPost<TUseCase>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var catalogEntry = KawaUseCaseCatalog.FromUseCaseType(typeof(TUseCase));
        var method = MapKawaPostFromContractMethod.MakeGenericMethod(
            catalogEntry.RequestType,
            catalogEntry.ResponseType);

        return (RouteHandlerBuilder)method.Invoke(null, [endpoints, pattern, catalogEntry])!;
    }

    private static RouteHandlerBuilder MapKawaPostFromContract<TRequest, TResponse>(
        IEndpointRouteBuilder endpoints,
        string pattern,
        KawaUseCaseCatalogEntry? catalogEntry = null)
        where TRequest : notnull
    {
        return endpoints.MapKawaPost<TRequest, TResponse>(pattern)
            .WithKawaUseCaseMetadata(catalogEntry);
    }

    private static RouteHandlerBuilder WithKawaPostOpenApiMetadata<TRequest, TResponse>(
        this RouteHandlerBuilder builder)
        where TRequest : notnull
    {
        return builder
            .Accepts<TRequest>("application/json")
            .Produces<TResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<KawaError>(StatusCodes.Status400BadRequest, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<KawaError>(StatusCodes.Status404NotFound, "application/json")
            .Produces<KawaError>(StatusCodes.Status409Conflict, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");
    }

    private static RouteHandlerBuilder WithKawaUseCaseMetadata(
        this RouteHandlerBuilder builder,
        KawaUseCaseCatalogEntry? catalogEntry)
    {
        if (catalogEntry is null)
        {
            return builder;
        }

        builder.WithMetadata(catalogEntry);

        if (!string.IsNullOrWhiteSpace(catalogEntry.Metadata.Name))
        {
            builder.WithName(catalogEntry.Metadata.Name);
        }

        if (!string.IsNullOrWhiteSpace(catalogEntry.Metadata.Summary))
        {
            builder.WithSummary(catalogEntry.Metadata.Summary);
        }

        if (!string.IsNullOrWhiteSpace(catalogEntry.Metadata.Description))
        {
            builder.WithDescription(catalogEntry.Metadata.Description);
        }

        if (catalogEntry.Metadata.Tags.Count > 0)
        {
            builder.WithTags(catalogEntry.Metadata.Tags.ToArray());
        }

        return builder;
    }
}
