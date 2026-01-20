using System.Text.Json.Nodes;

namespace Indice.Services;

/// <summary>
/// An interface for SignalR broadcasting service.
/// </summary>
public interface ISignalRProxyBroadcastService
{
    /// <summary>
    /// Broadcasts a command to a specific user.
    /// </summary>
    /// <param name="hub">The hub name.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="command">The command to broadcast.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task BroadcastToUserAsync( string hub, string userId, SignalRBroadcastCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Method to broadcast a command to all users.
    /// </summary>
    /// <param name="hub">The hub name.</param>
    /// <param name="command">The command to broadcast.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task BroadcastToUsersAsync( string hub, SignalRBroadcastCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Method to broadcast a command to a specific group.
    /// </summary>
    /// <param name="hub">The hub name.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="command">The command to broadcast.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task BroadcastToGroupAsync(string hub, string groupName, SignalRBroadcastCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Method to broadcast a command to a specific connection.
    /// </summary>
    /// <param name="hub">The hub name.</param>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="command">The command to broadcast.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task BroadcastToConnectionAsync(string hub, string connectionId, SignalRBroadcastCommand command, CancellationToken cancellationToken = default);

}

/// <summary>A command to broadcast a message.</summary>
/// <param name="Method">The method name to invoke.</param>
/// <param name="Message">The message to broadcast.</param>
public record SignalRBroadcastCommand([Description("The method name to invoke on the client.")] string Method, [Description("The message payload to broadcast.")] JsonNode Message);


/// <inheritdoc />
public class SignalRProxyBroadcastService : ISignalRProxyBroadcastService
{
    private readonly SignalRProxyHubContextStore _hubContextStore;

    /// <summary>
    /// Initializes a new instance of the SignalRBroadcastService class using the specified hub context store.
    /// </summary>
    /// <param name="hubContextStore"></param>
    public SignalRProxyBroadcastService(SignalRProxyHubContextStore hubContextStore) {
        _hubContextStore = hubContextStore;
    }

    /// <inheritdoc />
    public async Task BroadcastToUserAsync(
    string hub,
    string userId,
    SignalRBroadcastCommand command,
    CancellationToken cancellationToken = default) {

        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);
        await hubContext
            .Clients
            .User(userId)
                .SendCoreAsync(
                    method: command.Method,
                    args: [command.Message],
                    cancellationToken: cancellationToken);
    }
    /// <inheritdoc />
    public async Task BroadcastToUsersAsync(string hub, SignalRBroadcastCommand command, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);
        await hubContext
            .Clients
            .All
            .SendCoreAsync(
                method: command.Method,
                args: [command.Message],
                cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastToGroupAsync(string hub, string groupName, SignalRBroadcastCommand command, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);
        await hubContext.Clients.Groups([groupName]).SendCoreAsync(
                method: command.Method,
                args: [command.Message],
                cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastToConnectionAsync(string hub, string connectionId, SignalRBroadcastCommand command, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);
        await hubContext.Clients.Client(connectionId).SendCoreAsync(
                method: command.Method,
                args: [command.Message],
                cancellationToken: cancellationToken);
    }

}