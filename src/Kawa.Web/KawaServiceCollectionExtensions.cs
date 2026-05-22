using Kawa.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kawa.Web;

/// <summary>
/// Registers Kawa services with dependency injection.
/// </summary>
public static class KawaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core services required by Kawa endpoints.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddKawa(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<UseCaseExecutor>();

        return services;
    }
}
