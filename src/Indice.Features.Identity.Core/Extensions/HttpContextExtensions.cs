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
    /// <summary>Tries to resolve the device id using the <see cref="HttpContext"/>.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static async ValueTask<MfaDeviceIdentifier> ResolveDeviceIdAsync(this HttpContext? httpContext) {
        var request = httpContext?.Request;
        if (request is not null) {
            return new MfaDeviceIdentifier(await GetDeviceIdAsync(httpContext!), GetRegistrationId(httpContext!));
        }
        return MfaDeviceIdentifier.Empty;
    }

    private static async ValueTask<string?> GetDeviceIdAsync(HttpContext httpContext) {
        if (httpContext is null) {
            throw new ArgumentNullException(nameof(httpContext));
        }
        var deviceId = default(StringValues);
        var hasDeviceId = httpContext.Request.HasFormContentType && (
            httpContext.Request.Form.TryGetValue("DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue("Input.DeviceId", out deviceId) ||
            httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.DeviceId, out deviceId) 
        ) && MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        if (!hasDeviceId && httpContext.Items.TryGetValue("deviceId", out var deviceIdObject)) {
            deviceId = deviceIdObject?.ToString();
            hasDeviceId = MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        }
        if (!hasDeviceId) {
            deviceId = httpContext.User.FindFirstValue(BasicClaimTypes.DeviceId);
            hasDeviceId = MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        }
        if (!hasDeviceId) {
            var result = await httpContext.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
            deviceId = result.Principal?.FindFirstValue(BasicClaimTypes.DeviceId);
            hasDeviceId = MfaDeviceIdentifier.ValidateDeviceId(deviceId);
        }
        return hasDeviceId ? deviceId.ToString().Trim() : default;
    }

    /// <summary>
    /// Validates the device id.
    /// </summary>
    /// <param name="deviceId">The input device id as a string</param>
    /// <returns>True if the device id is valid. Otherwize false</returns>
    private static bool ValidateDeviceId(string? deviceId) {
        // Do the flollowing checks:
        // - check for empty
        // - check for null
        // - check format can be either one of the following
        //   - valid guid (using Guid.TryParse)
        //   - a valid browser fingerprint format `{sha256}.{browerName}`.
        //     Usually fingerprint hash is a sha256 that is 64 characters long in string representation.
        //     Also in order to avoid colisions with same browser engine different vendors
        //     we suffix the browser fingerprint with the browser name separated with the dot `.` character.
        return !string.IsNullOrWhiteSpace(deviceId) &&
               (Guid.TryParse(deviceId, out _) ||
                deviceId.Length > 65 && deviceId.IndexOf('.') > 0 && deviceId.Split('.')[0].Length == 65);
    }

    private static Guid? GetRegistrationId(HttpContext httpContext) {
        var registrationId = default(Guid);
        var hasRegistrationId = httpContext.Request.HasFormContentType &&
                                httpContext.Request.Form.TryGetValue(RegistrationRequestParameters.RegistrationId, out var registrationIdText) &&
                                Guid.TryParse(registrationIdText, out registrationId);
        return hasRegistrationId ? registrationId : null;
    }
}
