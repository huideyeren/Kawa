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
        => Task.FromResult(KawaResult<SendResponse>.Success(new SendResponse { Accepted = true }));
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
