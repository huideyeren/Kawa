using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Kawa.Abstractions;

namespace Kawa.Core;

// BCL-only instrumentation: no dependency on Aspire or an OpenTelemetry SDK.
internal static class UseCaseTelemetry
{
    private static readonly ActivitySource Activities = new("Kawa.Core");
    private static readonly Meter Meter = new("Kawa.Core");
    private static readonly Counter<long> Calls = Meter.CreateCounter<long>("kawa.usecase.calls", "{call}");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("kawa.usecase.duration", "ms");

    internal static async Task<KawaResult<T>> RunAsync<T>(Func<Task<KawaResult<T>>> execute, Type type, string operation)
    {
        using var scope = new Execution(type, operation);
        try
        {
            var result = await execute().ConfigureAwait(false);
            if (result.IsFailure) scope.Fail(result.Error!.Kind.ToString());
            return result;
        }
        catch (Exception exception)
        {
            scope.Fail(exception is OperationCanceledException ? "cancelled" : "exception");
            throw;
        }
    }

    internal static async Task RunAsync(Func<Task> execute, Type type, string operation)
    {
        using var scope = new Execution(type, operation);
        try { await execute().ConfigureAwait(false); }
        catch (Exception exception)
        {
            scope.Fail(exception is OperationCanceledException ? "cancelled" : "exception");
            throw;
        }
    }

    internal static async IAsyncEnumerable<KawaResult<T>> Stream<T>(
        Func<IAsyncEnumerable<KawaResult<T>>> execute, Type type, string operation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var scope = new Execution(type, operation);
        IAsyncEnumerator<KawaResult<T>> enumerator;
        try { enumerator = execute().GetAsyncEnumerator(cancellationToken); }
        catch (Exception exception)
        {
            scope.Fail(exception is OperationCanceledException ? "cancelled" : "exception");
            throw;
        }

        try
        {
            while (await MoveNextAsync(enumerator, scope).ConfigureAwait(false))
            {
                var result = enumerator.Current;
                if (result.IsFailure) scope.Fail(result.Error!.Kind.ToString());
                yield return result;
                if (result.IsFailure) yield break;
            }
        }
        finally
        {
            try { await enumerator.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception)
            {
                scope.Fail(exception is OperationCanceledException ? "cancelled" : "exception");
                throw;
            }
        }
    }

    private static async ValueTask<bool> MoveNextAsync<T>(IAsyncEnumerator<T> enumerator, Execution scope)
    {
        try { return await enumerator.MoveNextAsync().ConfigureAwait(false); }
        catch (Exception exception)
        {
            scope.Fail(exception is OperationCanceledException ? "cancelled" : "exception");
            throw;
        }
    }

    private sealed class Execution : IDisposable
    {
        private readonly Activity? activity;
        private readonly long started = Stopwatch.GetTimestamp();
        private readonly string typeName;
        private readonly string operation;
        private string status = "success";

        internal Execution(Type type, string operation)
        {
            typeName = type.FullName ?? type.Name;
            this.operation = operation;
            activity = Activities.StartActivity(operation);
            activity?.SetTag("kawa.usecase", typeName);
            activity?.SetTag("kawa.operation", operation);
        }

        internal void Fail(string reason)
        {
            status = reason;
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.SetTag("kawa.status", reason);
        }

        public void Dispose()
        {
            var tags = new TagList
            {
                { "kawa.usecase", typeName }, { "kawa.operation", operation }, { "kawa.status", status },
            };
            Calls.Add(1, tags);
            Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, tags);
            activity?.Dispose();
        }
    }
}
