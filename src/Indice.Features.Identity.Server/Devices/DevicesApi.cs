using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Devices;
using Indice.Features.Identity.Server.Devices.Models;
using Indice.Features.Identity.Server.Options;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Contains operations for managing user devices. 
/// Trusted Mobile devices and browsers. As well as push notifications.
/// </summary>
public static class DevicesApi
{
    /// <summary>
    /// Register operations for user devices. Trusted Mobile devices and browsers. As well as push notifications.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapManageDevices(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/my/devices");
        group.WithTags("Devices");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme));
        
             
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
             .WithParameterValidation<TrustDeviceRequest>();

        group.MapPut("{deviceId}/untrust", DeviceHandlers.UntrustDevice)
             .WithName(nameof(DeviceHandlers.UntrustDevice))
             .WithSummary("Sets a device as untrusted.");

        group.MapDelete("{deviceId}", DeviceHandlers.DeleteDevice)
             .WithName(nameof(DeviceHandlers.DeleteDevice))
             .WithSummary("Deletes the device.");

        return group;
    }
}
