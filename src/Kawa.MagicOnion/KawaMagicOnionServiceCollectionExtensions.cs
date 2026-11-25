using Kawa.Core;
using MagicOnion.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.MagicOnion;

/// <summary>
/// Registers Kawa's MagicOnion and transport-independent execution services.
/// </summary>
public static class KawaMagicOnionServiceCollectionExtensions
{
    /// <summary>
    /// Adds MagicOnion and the Kawa executors required by generated RPC adapters.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddKawaMagicOnion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMagicOnion();
        services.AddSingleton<UseCaseExecutor>();
        services.AddSingleton<ServerStreamingUseCaseExecutor>();
        services.AddSingleton<ClientStreamingUseCaseExecutor>();
        services.AddSingleton<DuplexStreamingUseCaseExecutor>();
        services.AddSingleton<StreamingHubUseCaseExecutor>();

        return services;
    }
}
