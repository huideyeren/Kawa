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
        return UseCaseTelemetry.RunAsync(() => useCase.OnConnectedAsync(session, cancellationToken),
            useCase.GetType(), "hub.connect");
    }

    /// <summary>
    /// Handles a client event for a hub connection.
    /// </summary>
    public Task HandleAsync<TClientEvent, TServerEvent>(IStreamingHubUseCase<TClientEvent, TServerEvent> useCase, TClientEvent clientEvent, IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(session);
        return UseCaseTelemetry.RunAsync(() => useCase.HandleAsync(clientEvent, session, cancellationToken),
            useCase.GetType(), "hub.handle");
    }

    /// <summary>
    /// Handles a hub connection closing.
    /// </summary>
    public Task OnDisconnectedAsync<TClientEvent, TServerEvent>(IStreamingHubUseCase<TClientEvent, TServerEvent> useCase, IKawaHubSession<TServerEvent> session, Exception? exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(session);
        return UseCaseTelemetry.RunAsync(() => useCase.OnDisconnectedAsync(session, exception, cancellationToken),
            useCase.GetType(), "hub.disconnect");
    }
}
