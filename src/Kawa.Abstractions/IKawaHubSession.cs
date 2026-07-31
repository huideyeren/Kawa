using System.Threading;
using System.Threading.Tasks;

namespace Kawa.Abstractions;

/// <summary>
/// Represents a transport-independent, typed connection to a real-time hub client.
/// </summary>
/// <typeparam name="TServerEvent">The event contract sent to hub clients.</typeparam>
public interface IKawaHubSession<TServerEvent>
{
    /// <summary>
    /// Gets the stable identifier of the connected client.
    /// </summary>
    string ConnectionId { get; }

    /// <summary>
    /// Sends an event to the connected client.
    /// </summary>
    Task SendAsync(TServerEvent serverEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an event to every client in the specified group.
    /// </summary>
    Task BroadcastToGroupAsync(string groupName, TServerEvent serverEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds this connection to a named group.
    /// </summary>
    Task JoinGroupAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes this connection from a named group.
    /// </summary>
    Task LeaveGroupAsync(string groupName, CancellationToken cancellationToken = default);
}
