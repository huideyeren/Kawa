using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa OpenAPI endpoints.
/// </summary>
public static class KawaOpenApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Kawa OpenAPI document endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The OpenAPI document route pattern.</param>
    /// <returns>The supplied endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapKawaOpenApi(
        this IEndpointRouteBuilder endpoints,
        string pattern = KawaOpenApiDefaults.DocumentPattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        endpoints.MapOpenApi(pattern);

        return endpoints;
    }
}
