using System;
using IdentityModel;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Devices;
using Indice.Features.Identity.Server.Devices.Models;
using Indice.Features.Identity.Server.Options;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using UAParser;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Contains operations responsible for sending push notifications to devices.
/// </summary>
public static class PushNotificationsApi
{
    /// <summary>
    /// Register operations responsible for sending push notifications to devices.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapDevicePush(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/devices");
        group.WithTags("PushNotifications");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme));


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

    internal static void TrustDeviceRequiresOtp(RequireOtpPolicy policy) =>
        policy.AddState(async (ctx) => {
            var userManager = ctx.RequestServices.GetRequiredService<ExtendedUserManager<User>>();
            var user = await userManager.GetUserAsync(ctx.User);
            if (user is null)
                return null;
            var deviceIdSpecified = ctx.Request.RouteValues.TryGetValue("deviceId", out var deviceId);
            if (!deviceIdSpecified) {
                return null;
            }
            return await userManager.GetDeviceByIdAsync(user, deviceId.ToString());
        })
        .AddMessageTemplate((sp, principal, state) =>
            sp.GetRequiredService<IdentityMessageDescriber>()
              .TrustedDeviceRequiresOtpMessage((UserDevice)state)
        )
        .AddPurpose((sp, principal, sub, phone, state) =>
            $"{nameof(DeviceHandlers.TrustDevice)}:{sub}:{phone}:{((UserDevice)state).Id}"
        )
        .AddValidator((sp, principal, state) =>
            state is null ? null : ValidationErrors.AddError("deviceId", "User or Device does not exist.")
        );
}
