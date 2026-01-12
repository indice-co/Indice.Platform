using System.Security.Claims;
using Indice.AspNetCore.Features.SignalREndpoint.Interfaces;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalREndpoint;

internal static class SignalREndpointsHandler
{
    public static async Task<Results<Ok<NegotiateResponse>, NotFound<string>>> Negotiate(
        string hub,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken,
        ISignalRNegotiateService signalRNegotiateService,
        IOptions<SignalREndpointsOptions> options)
    {
        if(options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.NotFound($"The hub '{hub}' is not recognized.");
        }
        var userId = currentUser.FindSubjectId();
        var claimsList = new List<Claim>();
        var response = await signalRNegotiateService.Negotiate(hub, userId!, claimsList, cancellationToken);
        return TypedResults.Ok(response);
    }

    public static async Task<Results<Ok, NotFound<string>>> AddUserToGroups(
    string hub,
    ClaimsPrincipal currentUser,
    List<string> groups,
    CancellationToken cancellationToken,
    ISignalRNegotiateService signalRNegotiateService,
    IOptions<SignalREndpointsOptions> options) {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.NotFound($"The hub '{hub}' is not recognized.");
        }
        var userId = currentUser.FindSubjectId();
        await signalRNegotiateService.AddUserToGroups(hub, groups, userId!, cancellationToken);
        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<string>, NotFound<string>>> BroadcastToUser(
        string hub,
        string userId,
        BroadcastCommand command,
        CancellationToken cancellationToken,
        ISignalRBroadcastService signalBroadcastService,
        IOptions<SignalREndpointsOptions> options) {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.NotFound($"The hub '{hub}' is not recognized.");
        }
        await signalBroadcastService.BroadcastToUser(hub, userId, command, cancellationToken);
        return TypedResults.Ok("sent");
    }

    #region Descriptions
    public static readonly string NEGOTIATE = @"
Returns the proper credentials to listen to a hub.

Parameters:
- hub: The name of the SignalR hub to connect to.
- currentUser: The authenticated user's claims principal.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string BROADCASTTOUSER = @"
Broadcasts message to specified user.

Parameters:
- hub: The name of the SignalR hub to connect to.
- userId: The ID of the user to broadcast the message to.
- command: The method and message sent.
- cancellationToken: Cancellation token for the async operation.";
    #endregion
}

/// <summary>
/// The Negotiate response.
/// <param name="url"></param>
/// <param name="accessToken"> The access token for the request. </param>
/// </summary>
public record NegotiateResponse(string? url, string? accessToken);

/// <summary>A command to broadcast a message.</summary>
/// <param name="Method">The method name to invoke.</param>
/// <param name="Message">The message to broadcast.</param>
public record BroadcastCommand(string Method,dynamic Message);
