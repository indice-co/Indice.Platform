using Indice.AspNetCore.Features.SignalREndpoint.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Indice.AspNetCore.Features.SignalREndpoint;

/// <inheritdoc />
public class SignalRBroadcastService : ISignalRBroadcastService {
    private readonly HubContextStore _hubContextStore;

    /// <summary>
    /// Initializes a new instance of the SignalRBroadcastService class using the specified hub context store.
    /// </summary>
    /// <param name="hubContextStore"></param>
    public SignalRBroadcastService(HubContextStore hubContextStore) {
        _hubContextStore = hubContextStore;
    }

    /// <inheritdoc />
    public async Task BroadcastToUser(
    string hub,
    string userId,
    BroadcastCommand command,
    CancellationToken cancellationToken) {

        var hubContext = await _hubContextStore.GetHubContextAsync(hub);
        await hubContext
            .Clients
            .User(userId)
                .SendCoreAsync(
                    method: "broadcastMessage",
                    args: ["system", command.Message],
                    cancellationToken: cancellationToken);
    }
    /// <inheritdoc />
    public async Task BroadcastToUsers(string hub, BroadcastCommand command, CancellationToken cancellationToken) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub);
        await hubContext
            .Clients
            .All
            .SendCoreAsync(
                method: "broadcastMessage",
                args: ["system", command.Message],
                cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastToGroup(string hub, string groupName, BroadcastCommand command, CancellationToken cancellationToken) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub);
        await hubContext.Clients.Groups(groupName).SendCoreAsync(
                method: "broadcastMessage",
                args: ["system", command.Message],
                cancellationToken: cancellationToken);
    }

}
