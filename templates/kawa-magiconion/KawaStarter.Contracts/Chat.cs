using System.Runtime.CompilerServices;
using Kawa.Abstractions;
using Kawa.MagicOnion;

namespace KawaStarter.Contracts;

[KawaMagicOnionService("chat")]
public sealed class SendUseCase : IUseCase<string, string>
{
    public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.IsNullOrWhiteSpace(request)
            ? KawaResult<string>.Failure(new KawaError(KawaErrorKind.Validation, "Message is required."))
            : KawaResult<string>.Success(request));
}

[KawaMagicOnionService("chat")]
public sealed class WatchUseCase : IServerStreamingUseCase<string, string>
{
    public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(
        string request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 1; i <= 2; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return KawaResult<string>.Success($"{request}-{i}");
            await Task.Yield();
        }
    }
}

[KawaMagicOnionService("chat")]
public sealed class UploadUseCase : IClientStreamingUseCase<string, int>
{
    public async Task<KawaResult<int>> ExecuteAsync(IAsyncEnumerable<string> requests, CancellationToken cancellationToken = default)
    {
        var count = 0;
        await foreach (var _ in requests.WithCancellation(cancellationToken)) count++;
        return KawaResult<int>.Success(count);
    }
}

[KawaMagicOnionService("chat")]
public sealed class ExchangeUseCase : IDuplexStreamingUseCase<string, string>
{
    public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(
        IAsyncEnumerable<string> requests, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in requests.WithCancellation(cancellationToken))
            yield return KawaResult<string>.Success(message);
    }
}

[KawaMagicOnionHub("chat")]
public sealed class ChatHubUseCase : IStreamingHubUseCase<string, string>
{
    public Task OnConnectedAsync(IKawaHubSession<string> session, CancellationToken cancellationToken = default) =>
        session.JoinGroupAsync("lobby", cancellationToken);
    public Task HandleAsync(string clientEvent, IKawaHubSession<string> session, CancellationToken cancellationToken = default) =>
        session.BroadcastToGroupAsync("lobby", clientEvent, cancellationToken);
    public Task OnDisconnectedAsync(IKawaHubSession<string> session, Exception? exception, CancellationToken cancellationToken = default) =>
        session.LeaveGroupAsync("lobby", cancellationToken);
}
