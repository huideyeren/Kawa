using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Kawa.Abstractions;
using Kawa.Core;

namespace Kawa.Core.Tests;

/// <summary>
/// Verifies use case execution through the core executor.
/// </summary>
public class UseCaseExecutorTests
{
    /// <summary>
    /// Verifies that the server-streaming executor passes each item through unchanged.
    /// </summary>
    [Fact]
    public async Task ServerStreamingExecuteAsync_ReturnsUseCaseItems()
    {
        var executor = new ServerStreamingUseCaseExecutor();
        var results = new List<KawaResult<string>>();

        await foreach (var result in executor.ExecuteAsync(new ExampleServerStreamingUseCase(), 2))
        {
            results.Add(result);
        }

        Assert.Collection(
            results,
            result => Assert.Equal("Value: 2", result.Value),
            result => Assert.Equal("Value: 3", result.Value));
    }

    /// <summary>
    /// Verifies that the client-streaming executor returns its use case result.
    /// </summary>
    [Fact]
    public async Task ClientStreamingExecuteAsync_ReturnsUseCaseResult()
    {
        var executor = new ClientStreamingUseCaseExecutor();

        var result = await executor.ExecuteAsync(new ExampleClientStreamingUseCase(), Values(2, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal("Total: 5", result.Value);
    }

    /// <summary>
    /// Verifies that the duplex-streaming executor passes each item through unchanged.
    /// </summary>
    [Fact]
    public async Task DuplexStreamingExecuteAsync_ReturnsUseCaseItems()
    {
        var executor = new DuplexStreamingUseCaseExecutor();
        var results = new List<KawaResult<string>>();

        await foreach (var result in executor.ExecuteAsync(new ExampleDuplexStreamingUseCase(), Values(4)))
        {
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Equal("Value: 4", results[0].Value);
    }

    /// <summary>
    /// Verifies that the hub executor forwards a typed event to the use case.
    /// </summary>
    [Fact]
    public async Task StreamingHubHandleAsync_ForwardsEventAndSession()
    {
        var useCase = new ExampleStreamingHubUseCase();
        var session = new ExampleHubSession();
        var executor = new StreamingHubUseCaseExecutor();

        await executor.HandleAsync(useCase, "hello", session);

        Assert.Equal("hello", useCase.LastEvent);
        Assert.Same(session, useCase.LastSession);
    }
    /// <summary>
    /// Verifies that the executor returns a successful use case result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ReturnsUseCaseResult()
    {
        var useCase = new ExampleUseCase();
        var executor = new UseCaseExecutor();

        var result = await executor.ExecuteAsync(useCase, 42, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Value: 42", result.Value);
    }

    /// <summary>
    /// Verifies that the executor returns a failed use case result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PropagatesFailureResult()
    {
        var useCase = new FailingUseCase();
        var executor = new UseCaseExecutor();

        var result = await executor.ExecuteAsync(useCase, 42, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Equal("Invalid input", result.Error.Message);
    }

    /// <summary>
    /// Verifies that the executor rejects a missing use case.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ThrowsForNullUseCase()
    {
        var executor = new UseCaseExecutor();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => executor.ExecuteAsync((IUseCase<int, string>)null!, 42));

        Assert.Equal("useCase", exception.ParamName);
    }

    private sealed class ExampleUseCase : IUseCase<int, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(int request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KawaResult<string>.Success($"Value: {request}"));
        }
    }

    private sealed class FailingUseCase : IUseCase<int, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(int request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KawaResult<string>.Failure(new KawaError(KawaErrorKind.Validation, "Invalid input")));
        }
    }

    private sealed class ExampleServerStreamingUseCase : IServerStreamingUseCase<int, string>
    {
        public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(int request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return KawaResult<string>.Success($"Value: {request}");
            await Task.Yield();
            yield return KawaResult<string>.Success($"Value: {request + 1}");
        }
    }

    private sealed class ExampleClientStreamingUseCase : IClientStreamingUseCase<int, string>
    {
        public async Task<KawaResult<string>> ExecuteAsync(IAsyncEnumerable<int> requests, CancellationToken cancellationToken = default)
        {
            var total = 0;

            await foreach (var request in requests.WithCancellation(cancellationToken))
            {
                total += request;
            }

            return KawaResult<string>.Success($"Total: {total}");
        }
    }

    private sealed class ExampleDuplexStreamingUseCase : IDuplexStreamingUseCase<int, string>
    {
        public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(IAsyncEnumerable<int> requests, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var request in requests.WithCancellation(cancellationToken))
            {
                yield return KawaResult<string>.Success($"Value: {request}");
            }
        }
    }

    private sealed class ExampleStreamingHubUseCase : IStreamingHubUseCase<string, string>
    {
        public string? LastEvent { get; private set; }

        public IKawaHubSession<string>? LastSession { get; private set; }

        public Task OnConnectedAsync(IKawaHubSession<string> session, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleAsync(string clientEvent, IKawaHubSession<string> session, CancellationToken cancellationToken = default)
        {
            LastEvent = clientEvent;
            LastSession = session;
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync(IKawaHubSession<string> session, Exception? exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ExampleHubSession : IKawaHubSession<string>
    {
        public string ConnectionId => "test-connection";

        public Task SendAsync(string serverEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task BroadcastToGroupAsync(string groupName, string serverEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task JoinGroupAsync(string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LeaveGroupAsync(string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static async IAsyncEnumerable<int> Values(params int[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}
