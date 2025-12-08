using Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;
using Indice.SignalR.Endpoints;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Indice.AspNetCore.Features.SignalrEnpoint;

/// <inheritdoc />
public class SignalRBroadcastService : ISignalRBroadcastService {
    private readonly HubContextStore _hubContextStore;

    public SignalRBroadcastService(HubContextStore hubContextStore) {
        _hubContextStore = hubContextStore;
    }

    /// <inheritdoc />
    public async Task BroadcastToUser(
    string hub,
    string userId,
    BroadcastCommand command,
    CancellationToken cancellationToken) {

        var hubContext = await _hubContextStore.GetHubContextAsync(hub, CancellationToken.None);
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
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, CancellationToken.None);
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
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, CancellationToken.None);
        await hubContext.Clients.Groups(groupName).SendCoreAsync(
                method: "broadcastMessage",
                args: ["system", command.Message],
                cancellationToken: cancellationToken);
    }

}
