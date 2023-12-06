#nullable enable
using System.Text.Json;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Indice.Features.Identity.Core;

/// <summary>An implementation of <see cref="IDeviceIdResolver"/> where the device id is resolved from the <see cref="IFormCollection"/>.</summary>
public class DeviceIdResolverHttpContext : IDeviceIdResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="DeviceIdResolverHttpContext"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeviceIdResolverHttpContext(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public async Task<MfaDeviceIdentifier> Resolve() {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null) {
            return new MfaDeviceIdentifier(await GetDeviceId(request), GetRegistrationId(request));
        }
        return new MfaDeviceIdentifier(null);
    }

    private async Task<string?> GetDeviceId(HttpRequest request) {
        var deviceId = default(StringValues);
        var hasDeviceId = request.HasFormContentType && (
                            request.Form.TryGetValue("DeviceId", out deviceId) ||
                            request.Form.TryGetValue("Input.DeviceId", out deviceId) ||
                            request.Form.TryGetValue(RegistrationRequestParameters.DeviceId, out deviceId)
                          );
        if (_httpContextAccessor.HttpContext is not null) {
            if (!hasDeviceId) {
                hasDeviceId = _httpContextAccessor.HttpContext.Items.TryGetValue("deviceId", out var deviceIdObject);
                deviceId = deviceIdObject?.ToString();
            }
            if (!hasDeviceId) {
                try {

                    hasDeviceId = _httpContextAccessor.HttpContext.Session.TryGetValue("deviceId", out var deviceIdObject);
                    deviceId = deviceIdObject is not null
                        ? (await JsonSerializer.DeserializeAsync<MfaDeviceIdentifier>(new MemoryStream(deviceIdObject)))?.Value
                        : default;
                } catch (InvalidOperationException) { }
            }
        }
        return await Task.FromResult(hasDeviceId ? deviceId.ToString() : null);
    }

    private static Guid? GetRegistrationId(HttpRequest request) {
        var registrationId = default(Guid);
        var hasRegistrationId = request.HasFormContentType &&
                                request.Form.TryGetValue(RegistrationRequestParameters.RegistrationId, out var registrationIdText) &&
                                Guid.TryParse(registrationIdText, out registrationId);
        return hasRegistrationId ? registrationId : null;
    }
}

/// <summary>Models an MFA device identifier.</summary>
/// <param name="Value">The device id.</param>
/// <param name="RegistrationId">The device registration id.</param>
public record MfaDeviceIdentifier(string? Value, Guid? RegistrationId = null)
{
    /// <summary>Determines if there is a value for <see cref="RegistrationId"/>.</summary>
    public bool HasRegistrationId => RegistrationId.HasValue;
}
#nullable disable