using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

namespace Kawa.Aspire.Hosting;

/// <summary>Models Kawa projects as Aspire resources without launching them during registration.</summary>
public static class KawaDistributedApplicationBuilderExtensions
{
    /// <summary>Adds a MagicOnion project with a named HTTP/2 gRPC endpoint.</summary>
    public static IResourceBuilder<ProjectResource> AddKawaMagicOnionProject(
        this IDistributedApplicationBuilder builder, string name, string projectPath, int? targetPort = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        if (targetPort is int port)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(port, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);
        }

        return builder.AddProject(name, projectPath, options => options.ExcludeLaunchProfile = true)
            .WithHttpEndpoint(targetPort: targetPort, name: "grpc")
            .WithEndpoint("grpc", endpoint => endpoint.Transport = "http2")
            .WithEnvironment("ASPNETCORE_Kestrel__EndpointDefaults__Protocols", "Http2");
    }

    /// <summary>Adds an executable CLI project, preserving each argument as a separate token.</summary>
    public static IResourceBuilder<ProjectResource> AddKawaCliProject(
        this IDistributedApplicationBuilder builder, string name, string projectPath, string[]? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        return builder.AddProject(name, projectPath, options => options.ExcludeLaunchProfile = true)
            .WithArgs(arguments ?? []);
    }

    /// <summary>Adds gRPC endpoint discovery and an explicit startup dependency to a consumer.</summary>
    public static IResourceBuilder<T> WithKawaMagicOnionReference<T>(
        this IResourceBuilder<T> consumer, IResourceBuilder<ProjectResource> service)
        where T : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(service);
        return consumer.WithReference(service.GetEndpoint("grpc")).WaitFor(service);
    }
}
