using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Devices;
using Indice.Features.Identity.Server.Devices.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations responsible for sending push notifications to devices.</summary>
public static class PushNotificationsApi
{
    /// <summary>Register operations responsible for sending push notifications to devices.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapDevicePush(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/devices");
        group.WithTags("DevicePush");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("push", PushNotificationHandlers.SendPushNotification)
             .WithName(nameof(PushNotificationHandlers.SendPushNotification))
             .WithSummary("Sends a push notification.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeAdmin)
             .WithParameterValidation<SendPushNotificationRequest>();

        return group;
    }
}
