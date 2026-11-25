using System.Diagnostics;
using System.Diagnostics.Metrics;
using Kawa.Abstractions;

namespace Kawa.Core.Tests;

/// <summary>Checks transport-independent observability without exposing application data.</summary>
public sealed class UseCaseTelemetryTests
{
    /// <summary>Ensures genuine execution emits a trace and completion measurements.</summary>
    [Fact]
    public async Task ExecuteAsync_EmitsTelemetryWithoutPayloads()
    {
        Activity? captured = null;
        using var activities = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kawa.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.GetTagItem("kawa.usecase")?.ToString() == typeof(TelemetryUseCase).FullName)
                    captured = activity;
            },
        };
        ActivitySource.AddActivityListener(activities);
        var metrics = new List<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kawa.Core") meterListener.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => metrics.Add(instrument.Name));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) => metrics.Add(instrument.Name));
        listener.Start();

        var response = await new UseCaseExecutor().ExecuteAsync(new TelemetryUseCase(), "private-payload");

        Assert.Equal("private-payload", response.Value);
        Assert.NotNull(captured);
        Assert.DoesNotContain(captured.TagObjects, tag => tag.Value?.ToString() == "private-payload");
        Assert.Contains("kawa.usecase.calls", metrics);
        Assert.Contains("kawa.usecase.duration", metrics);
    }

    /// <summary>A failure terminates the stream and disposes its producer.</summary>
    [Fact]
    public async Task Stream_StopsAndDisposesAfterFailure()
    {
        var useCase = new FailingStream();
        var results = new List<KawaResult<string>>();
        await foreach (var result in new ServerStreamingUseCaseExecutor().ExecuteAsync(useCase, "input"))
            results.Add(result);
        Assert.Single(results);
        Assert.True(useCase.Disposed);
        Assert.False(useCase.Continued);
    }

    private sealed class FailingStream : IServerStreamingUseCase<string, string>
    {
        public bool Disposed;
        public bool Continued;
        public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(string request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                yield return KawaResult<string>.Failure(new KawaError(KawaErrorKind.Validation, "failed"));
                Continued = true;
                yield return KawaResult<string>.Success("must not escape");
                await Task.CompletedTask;
            }
            finally { Disposed = true; }
        }
    }

    private sealed class TelemetryUseCase : IUseCase<string, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
            Task.FromResult(KawaResult<string>.Success(request));
    }
}
