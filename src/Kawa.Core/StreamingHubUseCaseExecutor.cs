using System;
using System.Threading;
using System.Threading.Tasks;
using Kawa.Abstractions;

namespace Kawa.Core;

/// <summary>
/// Executes typed hub lifecycle and event handlers through Kawa's application flow boundary.
/// </summary>
public sealed class StreamingHubUseCaseExecutor
{
    /// <summary>
    /// Handles a new hub connection.
    /// </summary>
    public Task OnConnectedAsync<TClientEvent, TServerEvent>(IStreamingHubUseCase<TClientEvent, TServerEvent> useCase, IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(session);
        return useCase.OnConnectedAsync(session, cancellationToken);
    }

    /// <summary>
    /// Handles a client event for a hub connection.
    /// </summary>
    public Task HandleAsync<TClientEvent, TServerEvent>(IStreamingHubUseCase<TClientEvent, TServerEvent> useCase, TClientEvent clientEvent, IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(session);
        return useCase.HandleAsync(clientEvent, session, cancellationToken);
    }

    /// <summary>
    /// Handles a hub connection closing.
    /// </summary>
    public Task OnDisconnectedAsync<TClientEvent, TServerEvent>(IStreamingHubUseCase<TClientEvent, TServerEvent> useCase, IKawaHubSession<TServerEvent> session, Exception? exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(session);
        return useCase.OnDisconnectedAsync(session, exception, cancellationToken);
    }
}
