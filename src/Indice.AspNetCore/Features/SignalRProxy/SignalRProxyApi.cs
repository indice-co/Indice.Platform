using Indice.AspNetCore.Features.SignalRProxy;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Endpoint mappings for SignalR.</summary>
public static class SignalRProxyApi
{

    /// <summary>Maps SignalR endpoints.</summary>
    /// <param name="routes">The endpoint route builder.</param>
    public static RouteGroupBuilder MapSignalRProxy(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<SignalRProxyOptions>>().Value;
        var group = routes.MapGroup($"{options.EndpointRoutePattern}");
        group.WithTags("SignalR");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser());

        if (options.ExcludeFromDescription) {
            group.ExcludeFromDescription();
        }
        group.AddOpenApiSecurityRequirement("oauth2")
             .WithOpenApiSecurityRequirement("oauth2");

        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // my endpoints
        var negotiateEndpoint = group.MapPost("hubs/{hubName}/negotiate", SignalRProxyHandlers.Negotiate)
            .WithDescription(SignalRProxyHandlers.NEGOTIATE)
            .WithSummary("Negotiate a SignalR connection for the authenticated user.")
            .WithName(nameof(SignalRProxyHandlers.Negotiate)).AllowAnonymous();
        if (options.NegotiateAuthenticationSchemes.Any()) {
            negotiateEndpoint.RequireAuthorization(pb => pb.RequireAuthenticatedUser().AddAuthenticationSchemes(options.NegotiateAuthenticationSchemes.ToArray()));
        }
        group.MapPost("hubs/{hubName}/groups/join/me", SignalRProxyHandlers.JoinGroups)
            .WithDescription(SignalRProxyHandlers.JOINGROUPS)
            .WithSummary("Join the current user to a SignalR group")
            .WithName(nameof(SignalRProxyHandlers.JoinGroups)).AllowAnonymous();

        group.MapPost("hubs/{hubName}/groups/leave/me", SignalRProxyHandlers.LeaveGroups)
            .WithDescription(SignalRProxyHandlers.LEAVEGROUPS)
            .WithSummary("Remove the current user from a SignalR group")
            .WithName(nameof(SignalRProxyHandlers.LeaveGroups)).AllowAnonymous();

        // management endpoints
        group.MapPost("hubs/{hubName}/groups/{groupName}/join/{userId}", SignalRProxyHandlers.AddUserToGroup)
            .WithDescription(SignalRProxyHandlers.ADDUSERTOGROUP)
            .WithSummary("Add a user to a SignalR group.")
            .WithName(nameof(SignalRProxyHandlers.AddUserToGroup))
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));

        group.MapPost("hubs/{hubName}/groups/{groupName}/leave/{userId}", SignalRProxyHandlers.RemoveUserFromGroup)
            .WithDescription(SignalRProxyHandlers.REMOVEUSERFROMGROUP)
            .WithSummary("Remove a user from a SignalR group.")
            .WithName(nameof(SignalRProxyHandlers.RemoveUserFromGroup))
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));


        group.MapPost("hubs/{hubName}/users/{userId}/broadcast", SignalRProxyHandlers.BroadcastToUser)
            .WithDescription(SignalRProxyHandlers.BROADCASTTOUSER)
            .WithSummary("Broadcast a message to a specific user.")
            .WithName(nameof(SignalRProxyHandlers.BroadcastToUser)).WithExampleRequestBody( new { Method = "broadcast", Message = "Hello!" })
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));

        group.MapPost("hubs/{hubName}/connections/{connectionId}/broadcast", SignalRProxyHandlers.BroadcastToConnection)
            .WithDescription(SignalRProxyHandlers.BROADCASTTOCONNECTION)
            .WithSummary("Broadcast a message to a specific connection.")
            .WithName(nameof(SignalRProxyHandlers.BroadcastToConnection)).WithExampleRequestBody(new { Method = "broadcast", Message = "Hello!" })
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));

        group.MapPost("hubs/{hubName}/groups/{groupName}/broadcast", SignalRProxyHandlers.BroadcastToGroup)
            .WithDescription(SignalRProxyHandlers.BROADCASTTOGROUP)
            .WithSummary("Broadcast a message to a SignalR group.")
            .WithName(nameof(SignalRProxyHandlers.BroadcastToGroup)).WithExampleRequestBody(new { Method = "broadcast", Message = "Hello!" })
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));

        return group;
    }
    
}
