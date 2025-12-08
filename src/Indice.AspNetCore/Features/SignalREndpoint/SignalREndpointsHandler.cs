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
        var userClaims = currentUser.Claims.ToList();
        var response = await signalRNegotiateService.Negotiate(hub, userId!, userClaims, cancellationToken);
        return TypedResults.Ok(response);
    }

    #region Descriptions
    public static readonly string NEGOTIATE = @"
Returns the proper credentials to listen to a hub.

Parameters:
- hub: The name of the SignalR hub to connect to.
- currentUser: The authenticated user's claims principal.
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
/// <param name="Message">The message to broadcast.</param>
public record BroadcastCommand(dynamic Message);
