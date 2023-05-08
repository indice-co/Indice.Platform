#nullable enable

using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core;

/// <summary>An implementation of <see cref="IMfaDeviceIdResolver"/> where the device id is resolved from the <see cref="IFormCollection"/>.</summary>
public class MfaDeviceIdResolverHttpContext : IMfaDeviceIdResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="MfaDeviceIdResolverHttpContext"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MfaDeviceIdResolverHttpContext(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Task<MfaDeviceIdentifier> Resolve() {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null && request.HasFormContentType && (
            request.Form.TryGetValue("DeviceId", out var deviceId) ||
            request.Form.TryGetValue("Input.DeviceId", out deviceId) ||
            request.Form.TryGetValue(RegistrationRequestParameters.DeviceId, out deviceId))
        ) {
            return Task.FromResult(new MfaDeviceIdentifier(deviceId));
        }
        if (request is not null &&
            request.HasFormContentType &&
            request.Form.TryGetValue(RegistrationRequestParameters.RegistrationId, out var registrationIdText) &&
            Guid.TryParse(registrationIdText, out var registrationId)
        ) {
            return Task.FromResult(new MfaDeviceIdentifier(null, registrationId));
        }
        return Task.FromResult(new MfaDeviceIdentifier(null));
    }
}

/// <summary></summary>
/// <param name="Value"></param>
/// <param name="RegistrationId"></param>
public record MfaDeviceIdentifier(string? Value, Guid? RegistrationId = null)
{
    /// <summary></summary>
    public bool HasRegistrationId => RegistrationId.HasValue;
}

#nullable disable