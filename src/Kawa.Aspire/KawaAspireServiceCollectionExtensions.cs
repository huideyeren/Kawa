using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Kawa.Aspire;

/// <summary>Registers optional service-side Aspire defaults without requiring an AppHost.</summary>
public static class KawaAspireServiceCollectionExtensions
{
    /// <summary>Enables Kawa/MagicOnion telemetry, health checks and HttpClient service discovery.</summary>
    /// <remarks>OTLP export is enabled only when an OTEL_EXPORTER_OTLP_ENDPOINT is configured.</remarks>
    public static IServiceCollection AddKawaAspireDefaults(
        this IServiceCollection services, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(RegistrationMarker))) return services;
        services.AddSingleton(new RegistrationMarker());

        var telemetry = services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource("Kawa.*", "MagicOnion.*"))
            .WithMetrics(metrics => metrics.AddMeter("Kawa.*", "MagicOnion.*"));
        var endpoint = configuration?["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            var configuredProtocol = configuration?["OTEL_EXPORTER_OTLP_PROTOCOL"]
                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
            var protocol = configuredProtocol switch
            {
                null or "" or "grpc" => OtlpExportProtocol.Grpc,
                "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                _ => throw new ArgumentException("OTEL_EXPORTER_OTLP_PROTOCOL must be grpc or http/protobuf."),
            };
            telemetry.UseOtlpExporter(protocol, new Uri(endpoint, UriKind.Absolute));
        }

        services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
        services.AddServiceDiscovery();
        services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());
        return services;
    }

    private sealed class RegistrationMarker;
}
