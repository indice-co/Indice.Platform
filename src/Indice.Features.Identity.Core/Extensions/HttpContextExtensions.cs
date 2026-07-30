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
    /// <summary>Tries to resolve the device id using the current http request. <see cref="HttpContext"/>.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static MfaDeviceIdentifier ResolveDeviceId(this HttpContext? httpContext) {
        var request = httpContext?.Request;
        if (request is not null) {
            return new MfaDeviceIdentifier(FindDeviceId(httpContext!), FindRegistrationId(httpContext!));
        }
        return MfaDeviceIdentifier.Empty;
    }

    /// <summary>Gets the current sign-in session id or creates one prefixed with <paramref name="prefix"/> (the originating auth flow).</summary>
    public static ValueTask<string?> GetOrCreateSessionId(this HttpContext httpContext, string prefix) {
        if (httpContext.Items.TryGetValue(HttpContextItemKeys.SessionId, out var value) && value is not null) {
            return new ValueTask<string?>(value.ToString());
        }
        
        var sessionId = $"{prefix}.{Guid.NewGuid()}";
        httpContext.Items[HttpContextItemKeys.SessionId] = sessionId;

        return new ValueTask<string?>(sessionId);
    }

    /// <summary>Tries to resolve the sign-in session id from the current HTTP request.</summary>
    public static string? GetSessionId(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(Indice.Features.Identity.Core.HttpContextItemKeys.SessionId, out var value) ? value?.ToString() : null;

    private static string? FindDeviceId(HttpContext httpContext) {
        ArgumentNullException.ThrowIfNull(httpContext);
        var deviceId = default(StringValues);
        var found = httpContext.Request.HasFormContentType && (
            httpContext.Request.Form.TryGetValue("DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue("Input.DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.DeviceId, out deviceId) 
        ) && MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        if (found) {
            return deviceId.ToString();
        }
        if (httpContext.Items.TryGetValue("deviceId", out var deviceIdObject) && MfaDeviceIdentifier.ValidateDeviceId(deviceIdObject?.ToString())) {
            return deviceIdObject!.ToString();
        }
        if (httpContext.User is not null) {
            return FindDeviceId(httpContext.User);
        }
        return null;
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
