using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Devices;
using Indice.Features.Identity.Server.Devices.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing user devices (trusted mobile devices and browsers) as well as push notifications.</summary>
public static class DevicesApi
{
    /// <summary>Register operations for user devices. Trusted Mobile devices and browsers. As well as push notifications.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapMyDevices(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/my/devices");
        group.WithTags("Devices");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("", DeviceHandlers.GetDevices)
             .WithName(nameof(DeviceHandlers.GetDevices))
             .WithSummary("Returns a list of registered user devices.");

        group.MapGet("{deviceId}", DeviceHandlers.GetDeviceById)
             .WithName(nameof(DeviceHandlers.GetDeviceById))
             .WithSummary("Gets a device by it's unique id.");

        group.MapPost("", DeviceHandlers.CreateDevice)
             .WithName(nameof(DeviceHandlers.CreateDevice))
             .WithSummary("Creates a new device and optionally registers for push notifications.")
             .WithParameterValidation<RegisterDeviceRequest>();

        group.MapPut("{deviceId}", DeviceHandlers.UpdateDevice)
             .WithName(nameof(DeviceHandlers.UpdateDevice))
             .WithSummary("Updates a device.")
             .WithParameterValidation<UpdateDeviceRequest>();

        group.MapPut("{deviceId}/trust", DeviceHandlers.TrustDevice)
             .WithName(nameof(DeviceHandlers.TrustDevice))
             .WithSummary("Starts the process of trusting a device.")
             .WithParameterValidation<TrustDeviceRequest>()
             .RequireOtp(TrustDeviceRequiresOtp);

        group.MapPut("{deviceId}/untrust", DeviceHandlers.UntrustDevice)
             .WithName(nameof(DeviceHandlers.UntrustDevice))
             .WithSummary("Sets a device as untrusted.");

        group.MapDelete("{deviceId}", DeviceHandlers.DeleteDevice)
             .WithName(nameof(DeviceHandlers.DeleteDevice))
             .WithSummary("Deletes the device.");

        return group;
    }

    internal static void TrustDeviceRequiresOtp(RequireOtpPolicy policy) =>
        policy.AddState(async httpContext => {
            var userManager = httpContext.RequestServices.GetRequiredService<ExtendedUserManager<User>>();
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) {
                return null;
            }
            var deviceIdSpecified = httpContext.Request.RouteValues.TryGetValue("deviceId", out var deviceId);
            if (!deviceIdSpecified) {
                return null;
            }
            return await userManager.GetDeviceByIdAsync(user, deviceId!.ToString());
        })
        .AddMessageTemplate((serviceProvider, principal, state) =>
            serviceProvider.GetRequiredService<IdentityMessageDescriber>().TrustedDeviceRequiresOtpMessage((UserDevice)state!)
        )
        .AddPurpose((serviceProvider, principal, sub, phone, state) =>
            $"{nameof(DeviceHandlers.TrustDevice)}:{sub}:{phone}:{((UserDevice)state!).Id}"
        )
        .AddValidator((sp, principal, state) =>
            state is null ? ValidationErrors.AddError("deviceId", "Device does not exist.") : ValidationErrors.Create()
        );
}
