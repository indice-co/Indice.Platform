using System.Security.Claims;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.Models;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Primitives;

namespace Indice.Features.Identity.Core.Extensions;

/// <summary>Helper methods on <see cref="HttpContent"/>.</summary>
public static class HttpContextExtensions
{
    /// <summary>Tries to resolve the device id using the current http request. <see cref="HttpContext"/>.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static MfaDeviceIdentifier ResolveDeviceId(this HttpContext? httpContext) {
        var request = httpContext?.Request;
        if (request is not null) {
            return new MfaDeviceIdentifier(FindDeviceId(httpContext!), FindRegistrationId(httpContext!));
        }
        return MfaDeviceIdentifier.Empty;
    }

    private static string? FindDeviceId(HttpContext httpContext) {
        ArgumentNullException.ThrowIfNull(httpContext);
        var deviceId = default(StringValues);
        var requestHasDeviceId = httpContext.Request.HasFormContentType && (
            httpContext.Request.Form.TryGetValue("DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue("Input.DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.DeviceId, out deviceId) 
        ) && MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        if (!requestHasDeviceId && httpContext.Items.TryGetValue("deviceId", out var deviceIdObject)) {
            deviceId = deviceIdObject?.ToString();
            requestHasDeviceId = MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        }
        if (!requestHasDeviceId) {
            deviceId = FindDeviceId(httpContext.User);
        }
        return requestHasDeviceId ? deviceId.ToString() : default;
    }

    private static string? FindDeviceId(ClaimsPrincipal claimsPrincipal) {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        var deviceId = claimsPrincipal.FindFirstValue(BasicClaimTypes.DeviceId);
        if (!MfaDeviceIdentifier.ValidateDeviceId(deviceId)) {
            return null;
        }
        return deviceId!.Trim();
    }

    private static Guid? FindRegistrationId(HttpContext httpContext) {
        var registrationId = default(Guid);
        var hasRegistrationId = httpContext.Request.HasFormContentType &&
                                httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.RegistrationId, out var registrationIdText) &&
                                Guid.TryParse(registrationIdText, out registrationId);
        return hasRegistrationId ? registrationId : null;
    }
}
