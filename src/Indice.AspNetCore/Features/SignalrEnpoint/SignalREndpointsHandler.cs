using System.Security.Claims;
using Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.SignalR.Endpoints
{
    internal static class SignalREndpointsHandler
    {
        public static async Task<Ok<NegotiateResponse>> Negotiate(
            string hub,
            ClaimsPrincipal currentUser,
            CancellationToken cancellationToken,
            ISignalRListenerService signalRListeningService)
        {
            var response = await signalRListeningService.Negotiate(hub, currentUser, cancellationToken);
            return TypedResults.Ok(response);
        }

        public static async Task<NoContent> BroadcastToUser(
            string hub,
            string userId,
            BroadcastCommand command,
            CancellationToken cancellationToken,
            ISignalRBroadcastingService signalRBroadcastingService)
        {
            await signalRBroadcastingService.BroadcastToUser(hub, userId, command, cancellationToken);
            return TypedResults.NoContent();
        }

        public static async Task<NoContent> BroadcastToUsers(
           string hub,
           BroadcastCommand command,
           ISignalRBroadcastingService signalRBroadcastingService,
           CancellationToken cancellationToken)
        {
            await signalRBroadcastingService.BroadcastToUsers(hub, command, cancellationToken);
            return TypedResults.NoContent();
        }

        public static async Task<NoContent> BroadcastToGroup(
            string hub,
            string groupName,
            BroadcastCommand command,
            ISignalRBroadcastingService signalRBroadcastingService,
            CancellationToken cancellationToken) {

            await signalRBroadcastingService.BroadcastToGroup(hub, groupName, command, cancellationToken);
            return TypedResults.NoContent();
        }
    }

    /// <summary>
    /// The Negotiate response.
    /// <param name="url"></param>
    /// <param name="accessToken"> The access token for the request. </param>
    /// </summary>
    public record NegotiateResponse(string? url, string? accessToken);

    /// <summary>A command to broadcast a message.</summary>
    /// <param name="Message">The message to broadcast.</param>
    public record BroadcastCommand(string Message);

}
