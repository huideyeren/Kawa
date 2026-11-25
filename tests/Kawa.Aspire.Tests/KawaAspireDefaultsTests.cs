using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Kawa.Abstractions;
using Kawa.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Kawa.Aspire.Tests;

public sealed class KawaAspireDefaultsTests
{
    [Fact]
    public async Task HealthEndpoints_SeparateReadinessAndLiveness()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = ["--hostBuilder:reloadConfigOnChange=false"],
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddKawaAspireDefaults();
        builder.Services.AddHealthChecks().AddCheck("database", () => HealthCheckResult.Unhealthy());
        await using var app = builder.Build();
        app.MapKawaAspireHealthChecks();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/alive")).StatusCode);
    }

    [Fact]
    public async Task Defaults_CaptureRealUseCaseTracesAndMetrics()
    {
        var traces = new ConcurrentBag<Activity>();
        var metricNames = new ConcurrentBag<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationManager());
        services.AddKawaAspireDefaults();
        services.AddOpenTelemetry()
            .WithTracing(trace => trace.AddProcessor(new SimpleActivityExportProcessor(new TraceExporter(traces))))
            .WithMetrics(metrics => metrics.AddReader(new PeriodicExportingMetricReader(
                new MetricExporter(metricNames), exportIntervalMilliseconds: int.MaxValue)));
        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<TracerProvider>();
        var meter = provider.GetRequiredService<MeterProvider>();

        await new UseCaseExecutor().ExecuteAsync(new ExampleUseCase(), "not-a-telemetry-tag");
        tracer.ForceFlush();
        meter.ForceFlush();

        Assert.Contains(traces, activity => activity.Source.Name == "Kawa.Core"
            && activity.GetTagItem("kawa.usecase")?.ToString() == typeof(ExampleUseCase).FullName);
        Assert.Contains("kawa.usecase.duration", metricNames);
        Assert.Contains("kawa.usecase.calls", metricNames);
        Assert.DoesNotContain(traces.SelectMany(activity => activity.TagObjects),
            tag => tag.Value?.ToString() == "not-a-telemetry-tag");
    }

    [Fact]
    public void Defaults_SubscribeToMagicOnionTelemetry()
    {
        var traces = new ConcurrentBag<Activity>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationManager());
        services.AddKawaAspireDefaults();
        services.AddOpenTelemetry().WithTracing(trace => trace.AddProcessor(
            new SimpleActivityExportProcessor(new TraceExporter(traces))));
        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<TracerProvider>();
        using (var source = new ActivitySource("MagicOnion.Server"))
        using (source.StartActivity("test-subscription")) { }
        tracer.ForceFlush();
        Assert.Contains(traces, activity => activity.Source.Name == "MagicOnion.Server");
    }

    [Fact]
    public async Task Discovery_ResolvesNamedEndpointThroughConfiguredHttpClient()
    {
        var services = new ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager();
        configuration["services:chat:http:0"] = "http://localhost:12345";
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration);
        services.AddLogging();
        services.AddKawaAspireDefaults();
        var observed = new ConcurrentBag<Uri>();
        services.AddHttpClient("chat").ConfigurePrimaryHttpMessageHandler(() => new CapturingHandler(observed));
        using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("chat");
        await client.GetAsync("http://chat/message");
        Assert.Contains(observed, uri => uri.Host == "localhost" && uri.Port == 12345 && uri.AbsolutePath == "/message");
    }

    private sealed class ExampleUseCase : IUseCase<string, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
            Task.FromResult(KawaResult<string>.Success(request));
    }

    private sealed class TraceExporter(ConcurrentBag<Activity> traces) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch) traces.Add(activity);
            return ExportResult.Success;
        }
    }

    private sealed class MetricExporter(ConcurrentBag<string> names) : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch) names.Add(metric.Name);
            return ExportResult.Success;
        }
    }

    private sealed class CapturingHandler(ConcurrentBag<Uri> observed) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            observed.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
