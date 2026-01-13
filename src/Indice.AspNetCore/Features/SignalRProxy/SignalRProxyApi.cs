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
        var negotiateEndpoint = group.MapPost("{hub}/negotiate", SignalRProxyHandlers.Negotiate)
            .WithDescription(SignalRProxyHandlers.NEGOTIATE)
            .WithSummary("SignalR negotiation endpoint.")
            .WithName(nameof(SignalRProxyHandlers.Negotiate));
        if (options.NegotiateAuthenticationSchemes.Any()) {
            negotiateEndpoint.RequireAuthorization(pb => pb.RequireAuthenticatedUser().AddAuthenticationSchemes(options.NegotiateAuthenticationSchemes.ToArray()));
        }
        group.MapPost("{hub}/groups/{groupName}/join/me", SignalRProxyHandlers.JoinGroup)
            .WithSummary("Join current user to a SignalR group.")
            .WithName(nameof(SignalRProxyHandlers.JoinGroup));

        // management endpoints
        group.MapPost("{hub}/groups/{groupName}/join/{userId}", SignalRProxyHandlers.AddUserToGroup)
            .WithSummary("Add a user to a SignalR group.")
            .WithName(nameof(SignalRProxyHandlers.AddUserToGroup))
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));


        group.MapPost("{hub}/users/{userId}/broadcast", SignalRProxyHandlers.BroadcastToUser)
            .WithDescription(SignalRProxyHandlers.BROADCASTTOUSER)
            .WithSummary("Broadcast a message to a SignalR user.")
            .WithName(nameof(SignalRProxyHandlers.BroadcastToUser))
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));

        group.MapPost("{hub}/groups/{groupName}/broadcast", SignalRProxyHandlers.BroadcastToGroup)
            .WithSummary("Broadcast a message to a SignalR group.")
            .WithName(nameof(SignalRProxyHandlers.BroadcastToGroup))
            .RequireAuthorization(x => x.RequireAssertion(ctx => ctx.User.IsSystemClient() || ctx.User.IsAdmin()));

        return group;
    }
}
