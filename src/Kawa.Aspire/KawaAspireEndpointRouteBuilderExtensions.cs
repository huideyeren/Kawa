using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Kawa.Aspire;

/// <summary>Explicitly maps service health checks for Aspire applications.</summary>
public static class KawaAspireEndpointRouteBuilderExtensions
{
    /// <summary>Maps readiness at /health and checks tagged live at /alive.</summary>
    public static IEndpointRouteBuilder MapKawaAspireHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapHealthChecks("/health");
        endpoints.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
        });
        return endpoints;
    }
}
