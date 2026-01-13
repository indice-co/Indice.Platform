using System.Security.Claims;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
namespace Indice.AspNetCore.Features.SignalRProxy;

internal static class SignalRProxyHandlers
{
    // this is a user present endpoint for self registering to signalr hubs.

    public static async Task<Results<Ok<SignalRNegotiationResponse>, ValidationProblem>> Negotiate(
        string hub,
        ClaimsPrincipal currentUser,
        ISignalRProxyNegotiateService signalRNegotiateService,
        IOptions<SignalRProxyOptions> options,
        CancellationToken cancellationToken) {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }
        var userId = currentUser.FindSubjectId();

        var groupNames = currentUser.Claims.Where(x => x.Type is not null && !string.IsNullOrWhiteSpace(x.Value))
                                           .Where(x => options.Value.ClaimTypesForAutoGroups.Contains(x.Type))
                                           .Select(x => options.Value.ClaimTypeToGroupName(x))
                                           .ToList();
        
        var response = await signalRNegotiateService.NegotiateAsync(hub, userId!, groupNames, cancellationToken);
        return TypedResults.Ok(response);
    }

    public static async Task<Results<NoContent, ValidationProblem>> JoinGroup(
    string hub,
    string groupName,
    ClaimsPrincipal currentUser,
    ISignalRProxyNegotiateService signalRNegotiateService,
    IOptions<SignalRProxyOptions> options,
    CancellationToken cancellationToken) {
        var errors = ValidationErrors.Create();
        var userId = currentUser.FindSubjectId();
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            errors.AddError(nameof(hub), $"The hub '{hub}' is not recognized.");
        }
        if (string.IsNullOrWhiteSpace(groupName)) {
            errors.AddError(nameof(groupName), "The groupName cannot be null or empty.");
        }
        if (string.IsNullOrWhiteSpace(userId)) {
            errors.AddError(nameof(userId), "The userId cannot be null or empty.");
        }
        if (errors.Count > 0) {
            return TypedResults.ValidationProblem(errors);
        }
        await signalRNegotiateService.AddUserToGroupsAsync(hub, userId!, [groupName], cancellationToken);
        return TypedResults.NoContent();
    }

    // these methods are meant to be used for administering purposes only. For example, adding a user to groups based on external events.

    public static async Task<Results<NoContent, ValidationProblem>> AddUserToGroup(
    string hub,
    string groupName,
    string userId,
    ISignalRProxyNegotiateService signalRNegotiateService,
    IOptions<SignalRProxyOptions> options,
    CancellationToken cancellationToken
    ) {
        var errors = ValidationErrors.Create();
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            errors.AddError(nameof(hub), $"The hub '{hub}' is not recognized.");
        }
        if (string.IsNullOrWhiteSpace(groupName)) {
            errors.AddError(nameof(groupName), "The groupName cannot be null or empty.");
        }
        if (string.IsNullOrWhiteSpace(userId)) {
            errors.AddError(nameof(userId), "The userId cannot be null or empty.");
        }
        if (errors.Count > 0) {
            return TypedResults.ValidationProblem(errors);
        }
        await signalRNegotiateService.AddUserToGroupsAsync(hub, userId!, [groupName], cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> BroadcastToUser(
        string hub,
        string userId,
        SignalRBroadcastCommand command,
        CancellationToken cancellationToken,
        ISignalRProxyBroadcastService signalBroadcastService,
        IOptions<SignalRProxyOptions> options) {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }
        await signalBroadcastService.BroadcastToUserAsync(hub, userId, command, cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> BroadcastToGroup(
        string hub,
        string groupName,
        SignalRBroadcastCommand command,
        CancellationToken cancellationToken,
        ISignalRProxyBroadcastService signalBroadcastService,
        IOptions<SignalRProxyOptions> options) {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }
        await signalBroadcastService.BroadcastToGroupAsync(hub, groupName, command, cancellationToken);
        return TypedResults.NoContent();
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