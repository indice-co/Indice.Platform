using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace Indice.AspNetCore.Features.SignalRProxy;

internal static class SignalRProxyHandlers
{
    // this is a user present endpoint for self registering to signalr hubs.

    //here I want to add a from query parameters groups to join automatically
    //the groupNames below are different types of groups that the user belongs to based on claims
    public static async Task<Results<Ok<SignalRNegotiationResponse>, ValidationProblem>> Negotiate(
        [Description("The name of the SignalR hub to connect to.")] string hub,
        [FromQuery(Name = "gps")] [Description("Optional array of group names to join upon connection.")] string[]? groupNames,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ISignalRProxyNegotiatiationService signalRNegotiateService,
        IOptions<SignalRProxyOptions> options,
        ISignalRProxyUserIdResolver userIdResolver,
        CancellationToken cancellationToken)
    {

        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }
        var userId = userIdResolver.Resolve(httpContext, currentUser);
        var autoGroupNames = currentUser.Claims.Where(x => x.Type is not null && !string.IsNullOrWhiteSpace(x.Value))
                                           .Where(x => options.Value.ClaimTypesForAutoGroups.Contains(x.Type))
                                           .Select(x => options.Value.ClaimTypeToGroupName(x))
                                           .ToList();
        if (groupNames is not null && groupNames.Any()) {
            autoGroupNames.AddRange(groupNames);
        }

        // Validate group names if validator is registered
        var validationError = await ValidateGroupNamesAsync(httpContext, autoGroupNames);
        if (validationError is not null) {
            return validationError;
        }

        var response = await signalRNegotiateService.NegotiateAsync(hub, autoGroupNames, userId, cancellationToken);
        return TypedResults.Ok(response);
    }

    public static async Task<Results<NoContent, ValidationProblem>> JoinGroups(
        [Description("The name of the SignalR hub.")] string hub,
        [FromQuery(Name = "gps")] [Description("Array of group names to join.")] string[] groupNames,
        [FromHeader(Name = "X-Connection-ID")] [Description("The connection ID of the SignalR client.")] string? connectionId,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ISignalRProxyNegotiatiationService signalRNegotiateService,
        IOptions<SignalRProxyOptions> options,
        ISignalRProxyUserIdResolver userIdResolver,
        CancellationToken cancellationToken)
    {

        var errors = ValidationErrors.Create();
        var userId = userIdResolver.Resolve(httpContext, currentUser);
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            errors.AddError(nameof(hub), $"The hub '{hub}' is not recognized.");
        }
        if (groupNames.Length == 0) {
            errors.AddError(nameof(groupNames), "The group names cannot be null or empty.");
        }
        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(connectionId)) {
            errors.AddError(nameof(connectionId), "Provide either a valid connection id or a valid authentication token.");
        }
        if (errors.Count > 0) {
            return TypedResults.ValidationProblem(errors);
        }

        // Validate group names if validator is registered
        var validationError = await ValidateGroupNamesAsync(httpContext, groupNames);
        if (validationError is not null) {
            return validationError;
        }

        if (!string.IsNullOrWhiteSpace(connectionId)) {
            await signalRNegotiateService.AddConnectionToGroupsAsync(hub, connectionId!, groupNames.ToList(), cancellationToken);
        }
        else {
            await signalRNegotiateService.AddUserToGroupsAsync(hub, userId!, groupNames.ToList(), cancellationToken);
        }
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> LeaveGroups(
        [Description("The name of the SignalR hub.")] string hub,
        [FromQuery(Name = "gps")] [Description("Array of group names to leave.")] string[] groupNames,
        [FromHeader(Name = "X-Connection-ID")] [Description("The connection ID of the SignalR client.")] string? connectionId,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ISignalRProxyNegotiatiationService signalRNegotiateService,
        IOptions<SignalRProxyOptions> options,
        ISignalRProxyUserIdResolver userIdResolver,
        CancellationToken cancellationToken)
    {

        var errors = ValidationErrors.Create();
        var userId = userIdResolver.Resolve(httpContext, currentUser);
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            errors.AddError(nameof(hub), $"The hub '{hub}' is not recognized.");
        }
        if (groupNames.Length == 0) {
            errors.AddError(nameof(groupNames), "The group names cannot be null or empty.");
        }
        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(connectionId)) {
            errors.AddError(nameof(connectionId), "Provide either a valid connection id or a valid authentication token.");
        }
        if (errors.Count > 0) {
            return TypedResults.ValidationProblem(errors);
        }
        if (!string.IsNullOrWhiteSpace(connectionId)) {
            await signalRNegotiateService.RemoveConnectionFromGroupsAsync(hub, connectionId!, groupNames.ToList(), cancellationToken);
        }
        else {
            await signalRNegotiateService.RemoveUserFromGroupsAsync(hub, userId!, groupNames.ToList(), cancellationToken);
        }
        return TypedResults.NoContent();
    }

    // these methods are meant to be used for administering purposes only. For example, adding a user to groups based on external events.

    public static async Task<Results<NoContent, ValidationProblem>> AddUserToGroup(
        [Description("The name of the SignalR hub.")] string hub,
        [Description("The name of the group to add the user to.")] string groupName,
        [Description("The ID of the user to add to the group.")] string userId,
        HttpContext httpContext,
        ISignalRProxyNegotiatiationService signalRNegotiateService,
        IOptions<SignalRProxyOptions> options,
        CancellationToken cancellationToken)
    {
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

        // Validate group name if validator is registered
        var validationError = await ValidateGroupNameAsync(httpContext, groupName);
        if (validationError is not null) {
            return validationError;
        }

        await signalRNegotiateService.AddUserToGroupsAsync(hub, userId!, [groupName], cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> BroadcastToUser(
        [Description("The name of the SignalR hub to broadcast through.")] string hub,
        [Description("The ID of the user to broadcast the message to.")] string userId,
        [Description("The SignalR broadcast command containing the method name and message.")] SignalRBroadcastCommand command,
        CancellationToken cancellationToken,
        ISignalRProxyBroadcastService signalBroadcastService,
        IOptions<SignalRProxyOptions> options)
    {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }
        await signalBroadcastService.BroadcastToUserAsync(hub, userId, command, cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> BroadcastToGroup(
        [Description("The name of the SignalR hub to broadcast through.")] string hub,
        [Description("The name of the group to broadcast to.")] string groupName,
        [Description("The SignalR broadcast command containing the method name and message.")] SignalRBroadcastCommand command,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        ISignalRProxyBroadcastService signalBroadcastService,
        IOptions<SignalRProxyOptions> options)
    {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }

        // Validate group name if validator is registered
        var validationError = await ValidateGroupNameAsync(httpContext, groupName);
        if (validationError is not null) {
            return validationError;
        }

        await signalBroadcastService.BroadcastToGroupAsync(hub, groupName, command, cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> BroadcastToConnection(
        [Description("The name of the SignalR hub to broadcast through.")] string hub,
        [Description("The connection ID to broadcast the message to.")] string connectionId,
        [Description("The SignalR broadcast command containing the method name and message.")] SignalRBroadcastCommand command,
        CancellationToken cancellationToken,
        ISignalRProxyBroadcastService signalBroadcastService,
        IOptions<SignalRProxyOptions> options)
    {
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hub), $"The hub '{hub}' is not recognized."));
        }
        await signalBroadcastService.BroadcastToConnectionAsync(hub, connectionId, command, cancellationToken);
        return TypedResults.NoContent();
    }


    public static async Task<Results<NoContent, ValidationProblem>> RemoveUserFromGroup(
        [Description("The name of the SignalR hub.")] string hub,
        [Description("The name of the group to remove the user from.")] string groupName,
        [Description("The ID of the user to remove from the group.")] string userId,
        ISignalRProxyNegotiatiationService signalRNegotiateService,
        IOptions<SignalRProxyOptions> options,
        CancellationToken cancellationToken)
    {

        var errors = ValidationErrors.Create();
        if (options.Value.AllowedHubs is null || !options.Value.AllowedHubs.Contains(hub)) {
            errors.AddError(nameof(hub), $"The hub '{hub}' is not recognized.");
        }
        if (string.IsNullOrWhiteSpace(groupName)) {
            errors.AddError(nameof(groupName), "The groupName cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(userId)) {
            errors.AddError(nameof(userId), "The userId cannot be null or empty.");
        }
        if (errors.Count > 0) {
            return TypedResults.ValidationProblem(errors);
        }
        await signalRNegotiateService.RemoveUserFromGroupsAsync(hub, userId!, [groupName], cancellationToken);
        return TypedResults.NoContent();
    }

    #region Helper Methods
    /// <summary>
    /// Validates a single group name using the registered validator if available.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="groupName">The group name to validate.</param>
    /// <returns>A ValidationProblem result if validation fails, otherwise null.</returns>
    private static async Task<ValidationProblem?> ValidateGroupNameAsync(HttpContext httpContext, string groupName)
    {
        var groupValidator = httpContext.RequestServices.GetService<ISignalRProxyGroupNameValidator>();
        if (groupValidator is not null) {
            try {
                await groupValidator.ValidateAsync(groupName);
            } catch (ValidationException ex) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(groupName), ex.Message));
            } catch (Exception ex) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(groupName), $"Group validation failed: {ex.Message}"));
            }
        }
        return null;
    }

    /// <summary>
    /// Validates multiple group names using the registered validator if available.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="groupNames">The group names to validate.</param>
    /// <returns>A ValidationProblem result if validation fails, otherwise null.</returns>
    private static async Task<ValidationProblem?> ValidateGroupNamesAsync(HttpContext httpContext, IEnumerable<string> groupNames)
    {
        var groupValidator = httpContext.RequestServices.GetService<ISignalRProxyGroupNameValidator>();
        if (groupValidator is not null) {
            var errors = ValidationErrors.Create();
            foreach (var groupName in groupNames) {
                try {
                    await groupValidator.ValidateAsync(groupName);
                } catch (ValidationException ex) {
                    errors.AddError(nameof(groupNames), $"Group '{groupName}': {ex.Message}");
                } catch (Exception ex) {
                    errors.AddError(nameof(groupNames), $"Group '{groupName}' validation failed: {ex.Message}");
                }
            }
            if (errors.Count > 0) {
                return TypedResults.ValidationProblem(errors);
            }
        }
        return null;
    }
    #endregion

    #region Descriptions
    public static readonly string NEGOTIATE = @"
Initiates a SignalR connection negotiation for the authenticated user and returns connection credentials.

Parameters:
- hub: The name of the SignalR hub to connect to.
- groupNames: Optional query parameter specifying additional groups to join upon connection.
- currentUser: The authenticated user's claims principal.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string JOINGROUPS = @"
Adds the current user or connection to one or more SignalR groups.

Parameters:
- hub: The name of the SignalR hub.
- groupNames: Query parameter specifying one or more group names to join (array of group names).
- currentUser: The authenticated user's claims principal.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string LEAVEGROUPS = @"
Removes the current user or connection from one or more SignalR groups.
Parameters:
- hub: The name of the SignalR hub.
- groupNames: Query parameter specifying one or more group names to leave (array of group names).
- currentUser: The authenticated user's claims principal.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string ADDUSERTOGROUP = @"
Adds a specified user to a SignalR group (admin/system only).

Parameters:
- hub: The name of the SignalR hub.
- groupName: The name of the group to add the user to.
- userId: The ID of the user to add to the group.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string BROADCASTTOUSER = @"
Broadcasts a message to a specific user across all their active SignalR connections (admin/system only).

Parameters:
- hub: The name of the SignalR hub to broadcast through.
- userId: The ID of the user to broadcast the message to.
- command: The SignalRBroadcastCommand containing the method name and arguments.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string BROADCASTTOGROUP = @"
Broadcasts a message to all users in a specific SignalR group (admin/system only).

Parameters:
- hub: The name of the SignalR hub to broadcast through.
- groupName: The name of the group to broadcast to.
- command: The SignalRBroadcastCommand containing the method name and arguments.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string BROADCASTTOCONNECTION = @"
Broadcasts a message to a specific connection (admin/system only).

Parameters:
- hub: The name of the SignalR hub to broadcast through.
- connectionId: The connection ID to broadcast the message to.
- command: The SignalRBroadcastCommand containing the method name and arguments.
- cancellationToken: Cancellation token for the async operation.";

    public static readonly string REMOVEUSERFROMGROUP = @"
Removes a specified user from a SignalR group (admin/system only).
Parameters:
- hub: The name of the SignalR hub.
- groupName: The name of the group to remove the user from.
- userId: The ID of the user to remove from the group.
- cancellationToken: Cancellation token for the async operation.";
    #endregion
}