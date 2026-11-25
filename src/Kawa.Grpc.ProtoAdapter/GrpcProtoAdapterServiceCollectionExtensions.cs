using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Grpc.ProtoAdapter;

/// <summary>
/// Registers explicit protobuf-to-Kawa contract adapters.
/// </summary>
public static class GrpcProtoAdapterServiceCollectionExtensions
{
    /// <summary>
    /// Registers an application-owned adapter for an existing protobuf type and Kawa contract.
    /// </summary>
    /// <typeparam name="TAdapter">The explicit adapter implementation.</typeparam>
    /// <typeparam name="TProto">The generated or hand-authored protobuf type.</typeparam>
    /// <typeparam name="TContract">The transport-independent Kawa contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddGrpcProtoAdapter<TAdapter, TProto, TContract>(this IServiceCollection services)
        where TAdapter : class, IGrpcProtoAdapter<TProto, TContract>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IGrpcProtoAdapter<TProto, TContract>, TAdapter>();
        return services;
    }
}
