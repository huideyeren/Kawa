using Kawa.Abstractions;
using Kawa.Grpc;

namespace KawaStarter.ProtoContracts;

/// <summary>
/// Chat use cases whose contracts also generate a standard protobuf API.
/// </summary>
[KawaProtoService("Chat")]
public sealed class SendUseCase : IUseCase<SendRequest, SendResponse>
{
    /// <inheritdoc />
    public Task<KawaResult<SendResponse>> ExecuteAsync(SendRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(string.IsNullOrWhiteSpace(request.Message)
            ? KawaResult<SendResponse>.Failure(new KawaError(KawaErrorKind.Validation, "Message is required."))
            : KawaResult<SendResponse>.Success(new SendResponse { Message = request.Message }));
}

/// <summary>
/// The request contract exported by the chat example.
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
/// The response contract exported by the chat example.
/// </summary>
public sealed class SendResponse
{
    /// <summary>
    /// Gets the accepted chat message.
    /// </summary>
    [KawaProtoField(1)]
    public string Message { get; init; } = string.Empty;
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
