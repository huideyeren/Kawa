using Kawa.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Grpc;

/// <summary>
/// Registers standard gRPC and the Kawa execution services required by generated adapters.
/// </summary>
public static class KawaGrpcServiceCollectionExtensions
{
    /// <summary>
    /// Adds standard gRPC and the transport-independent Kawa executors.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddKawaGrpc(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc();
        services.AddSingleton<UseCaseExecutor>();
        services.AddSingleton<ServerStreamingUseCaseExecutor>();
        services.AddSingleton<ClientStreamingUseCaseExecutor>();
        services.AddSingleton<DuplexStreamingUseCaseExecutor>();

        return services;
    }
}
