using System.Security.Claims;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Indice.Features.Identity.Core.Extensions;

/// <summary>Helper methods on <see cref="HttpContent"/>.</summary>
public static class HttpContextExtensions
{
    /// <summary>Tries to resolve the device id using the <see cref="HttpContext"/>.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static MfaDeviceIdentifier ResolveDeviceId(this HttpContext? httpContext) {
        var request = httpContext?.Request;
        if (request is not null) {
            return new MfaDeviceIdentifier(GetDeviceId(httpContext!), GetRegistrationId(httpContext!));
        }
        return MfaDeviceIdentifier.Empty;
    }

    private static string? GetDeviceId(HttpContext httpContext) {
        if (httpContext is null) {
            throw new ArgumentNullException(nameof(httpContext));
        }
        var deviceId = default(StringValues);
        var hasDeviceId = httpContext.Request.HasFormContentType && (
            httpContext.Request.Form.TryGetValue("DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue("Input.DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.DeviceId, out deviceId) 
        ) && !string.IsNullOrWhiteSpace(deviceId);
        if (!hasDeviceId && httpContext.Items.TryGetValue("deviceId", out var deviceIdObject)) {
            deviceId = deviceIdObject?.ToString();
            hasDeviceId = !string.IsNullOrWhiteSpace(deviceId);
        }
        if (!hasDeviceId) {
            deviceId = httpContext.User.FindFirstValue(BasicClaimTypes.DeviceId);
            hasDeviceId = !string.IsNullOrWhiteSpace(deviceId);
        }
        return hasDeviceId ? deviceId.ToString() : default;
    }

    private static Guid? GetRegistrationId(HttpContext httpContext) {
        var registrationId = default(Guid);
        var hasRegistrationId = httpContext.Request.HasFormContentType &&
                                httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.RegistrationId, out var registrationIdText) &&
                                Guid.TryParse(registrationIdText, out registrationId);
        return hasRegistrationId ? registrationId : null;
    }
}
