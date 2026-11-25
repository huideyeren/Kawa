using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kawa.Abstractions;

/// <summary>
/// Defines lifecycle and typed event handling for a real-time hub connection.
/// </summary>
/// <typeparam name="TClientEvent">The event contract received from hub clients.</typeparam>
/// <typeparam name="TServerEvent">The event contract sent to hub clients.</typeparam>
public interface IStreamingHubUseCase<TClientEvent, TServerEvent>
{
    /// <summary>
    /// Handles a new hub connection.
    /// </summary>
    Task OnConnectedAsync(IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an event sent by the connected client.
    /// </summary>
    Task HandleAsync(TClientEvent clientEvent, IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a hub connection closing.
    /// </summary>
    Task OnDisconnectedAsync(IKawaHubSession<TServerEvent> session, Exception? exception, CancellationToken cancellationToken = default);
}
