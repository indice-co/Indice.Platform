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
    public Task<string> Resolve() {
        var request = _httpContextAccessor.HttpContext.Request;
        if (request.HasFormContentType && request.Form.TryGetValue("DeviceId", out var deviceId)) {
            return Task.FromResult((string)deviceId);
        }
        return Task.FromResult(default(string));
    }
}
