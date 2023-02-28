using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core;

/// <summary>An implementation of <see cref="IMfaDeviceIdResolver"/> where the device id is resolved from the <see cref="IFormCollection"/>.</summary>
public class DefaultMfaDeviceIdResolver : IMfaDeviceIdResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="DefaultMfaDeviceIdResolver"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DefaultMfaDeviceIdResolver(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Task<string> Resolve() {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext.Request.Form.TryGetValue("DeviceId", out var deviceId)) {
            return Task.FromResult((string)deviceId);
        }
        return Task.FromResult(default(string));
    }
}
