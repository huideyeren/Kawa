using Kawa.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa API catalog endpoints.
/// </summary>
public static class KawaApiCatalogEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Kawa API catalog document endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The API catalog route pattern.</param>
    /// <returns>The supplied endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapKawaApiCatalog(
        this IEndpointRouteBuilder endpoints,
        string pattern = KawaOpenApiDefaults.ApiCatalogPattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        endpoints.MapGet(
            pattern,
            (EndpointDataSource endpointDataSource, IServiceProvider services) =>
            {
                var entries = endpointDataSource.Endpoints
                    .SelectMany(endpoint => endpoint.Metadata.OfType<KawaUseCaseCatalogEntry>().Select(entry => entry with
                    {
                        Transports = entry.Transports?.Select(transport => transport.Kind == "http" && endpoint is RouteEndpoint route
                            ? transport with { Route = route.RoutePattern.RawText } : transport).ToArray(),
                    }))
                    .Concat(services.GetServices<KawaUseCaseCatalogEntry>().Where(entry => entry.Transports is { Count: > 0 }));

                return Results.Ok(KawaApiCatalog.FromEntries(entries));
            })
            .WithName(KawaOpenApiDefaults.ApiCatalogEndpointName)
            .WithSummary("Get Kawa API catalog")
            .WithDescription("Returns the Kawa API catalog generated from use case contracts.")
            .Produces<KawaApiCatalog>(StatusCodes.Status200OK, "application/json");

        return endpoints;
    }
}
