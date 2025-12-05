using Indice.SignalR.Endpoints;

namespace Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;

/// <summary>
/// An interface for SignalR broadcasting service.
/// </summary>
public interface ISignalRBroadcastingService
{
    /// <summary>
    /// Broadcasts a command to a specific user.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="userId"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task BroadcastToUser(
        string hub,
        string userId,
        BroadcastCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to broadcast a command to all users.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task BroadcastToUsers(
       string hub,
       BroadcastCommand command,
       CancellationToken cancellationToken);

    /// <summary>
    /// Method to broadcast a command to a specific group.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="groupName"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task BroadcastToGroup(
        string hub,
        string groupName,
        BroadcastCommand command,
        CancellationToken cancellationToken);

}
