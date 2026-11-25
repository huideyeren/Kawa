using Kawa.Abstractions;
using Kawa.Grpc;

namespace Kawa.Grpc.BuildFixture;

/// <summary>
/// A build fixture that proves Kawa-originated contracts are compiled through Grpc.Tools.
/// </summary>
[KawaProtoService("Chat")]
public sealed class SendUseCase : IUseCase<SendRequest, SendResponse>
{
    /// <inheritdoc />
    public Task<KawaResult<SendResponse>> ExecuteAsync(SendRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(request.Message == "Tenryu"
            ? KawaResult<SendResponse>.Success(new SendResponse { Accepted = true })
            : KawaResult<SendResponse>.Failure(new KawaError(KawaErrorKind.Validation, "Message must be Tenryu.")));
}

/// <summary>
/// The request contract exported by the build fixture.
/// </summary>
public sealed class SendRequest
{
    /// <summary>
    /// Gets the chat message.
    /// </summary>
    [KawaProtoField(1)]
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// The response contract exported by the build fixture.
/// </summary>
public sealed class SendResponse
{
    /// <summary>
    /// Gets a value indicating whether the message was accepted.
    /// </summary>
    [KawaProtoField(1)]
    public bool Accepted { get; init; }
}

[KawaProtoService("Chat")]
public sealed class WatchUseCase : IServerStreamingUseCase<WatchRequest, WatchResponse>
{
    public async IAsyncEnumerable<KawaResult<WatchResponse>> ExecuteAsync(WatchRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return KawaResult<WatchResponse>.Success(new WatchResponse { Message = request.Message + "-1" });
        yield return KawaResult<WatchResponse>.Success(new WatchResponse { Message = request.Message + "-2" });
        await Task.CompletedTask;
    }
}

public sealed class WatchRequest { [KawaProtoField(1)] public string Message { get; init; } = string.Empty; }
public sealed class WatchResponse { [KawaProtoField(1)] public string Message { get; init; } = string.Empty; }

[KawaProtoService("Chat")]
public sealed class SumUseCase : IClientStreamingUseCase<SumRequest, SumResponse>
{
    public async Task<KawaResult<SumResponse>> ExecuteAsync(IAsyncEnumerable<SumRequest> requests, CancellationToken cancellationToken = default)
    {
        var sum = 0;
        await foreach (var request in requests.WithCancellation(cancellationToken)) sum += request.Value;
        return KawaResult<SumResponse>.Success(new SumResponse { Total = sum });
    }
}

public sealed class SumRequest { [KawaProtoField(1)] public int Value { get; init; } }
public sealed class SumResponse { [KawaProtoField(1)] public int Total { get; init; } }

[KawaProtoService("Chat")]
public sealed class EchoStreamUseCase : IDuplexStreamingUseCase<EchoRequest, EchoResponse>
{
    public async IAsyncEnumerable<KawaResult<EchoResponse>> ExecuteAsync(IAsyncEnumerable<EchoRequest> requests, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var request in requests.WithCancellation(cancellationToken)) yield return KawaResult<EchoResponse>.Success(new EchoResponse { Message = request.Message });
    }
}

public sealed class EchoRequest { [KawaProtoField(1)] public string Message { get; init; } = string.Empty; }
public sealed class EchoResponse { [KawaProtoField(1)] public string Message { get; init; } = string.Empty; }

[KawaProtoService("Chat")]
public sealed class RejectUseCase : IUseCase<SendRequest, SendResponse>
{
    public Task<KawaResult<SendResponse>> ExecuteAsync(SendRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(KawaResult<SendResponse>.Success(new SendResponse { Accepted = false }));
}
