using Indice.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using System.Security.Claims;

namespace Indice.SignalR.Endpoints
{
    internal static class SignalREndpointsHandler
    {
        public static async Task<Ok<NegotiateResponse>> Negotiate(
            string hub,
            ServiceManager serviceManager,
            ClaimsPrincipal currentUser,
            CancellationToken cancellationToken)
        {
            var hubContext = await serviceManager.CreateHubContextAsync(hub, cancellationToken);
            var negotiationResponse = await hubContext.NegotiateAsync(new NegotiationOptions
            {
                TokenLifetime = TimeSpan.FromHours(1),
                UserId = currentUser.FindSubjectId()
            });

            return TypedResults.Ok(new NegotiateResponse(
                negotiationResponse.Url,
                negotiationResponse.AccessToken)
                );
        }


        public static async Task<NoContent> BroadcastToUser(
            string hub,
            string userId,
            BroadcastCommand command,
            ServiceManager serviceManager,
            CancellationToken cancellationToken)
        {

            var hubContext = await serviceManager.CreateHubContextAsync(hub, cancellationToken);
            await hubContext
                .Clients
                .User(userId)
                    .SendCoreAsync(
                        method: "broadcastMessage",
                        args: ["system", command.Message],
                        cancellationToken: cancellationToken);

            return TypedResults.NoContent();
        }

        public static async Task<NoContent> BroadcastToUsers(
           string hub,
           BroadcastCommand command,
           ServiceManager serviceManager,
           CancellationToken cancellationToken)
        {
            var hubContext = await serviceManager.CreateHubContextAsync(hub, cancellationToken);
            await hubContext
                .Clients
                .All
                .SendCoreAsync(
                    method: "broadcastMessage",
                    args: ["system", command.Message],
                    cancellationToken: cancellationToken);
            return TypedResults.NoContent();
        }
    }

    public record NegotiateResponse(string? url, string? accessToken);

    /// <summary>A command to broadcast a message.</summary>
    /// <param name="Message">The message to broadcast.</param>
    public record BroadcastCommand(string Message);

}
