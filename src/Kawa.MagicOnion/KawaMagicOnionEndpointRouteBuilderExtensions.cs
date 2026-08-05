using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Kawa.MagicOnion;

/// <summary>
/// Maps the MagicOnion endpoints that host Kawa-generated adapters.
/// </summary>
public static class KawaMagicOnionEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps MagicOnion service endpoints for Kawa-generated adapters.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to configure.</param>
    /// <returns>The supplied endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapKawaMagicOnionServices(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapMagicOnionService();
        return endpoints;
    }
}
