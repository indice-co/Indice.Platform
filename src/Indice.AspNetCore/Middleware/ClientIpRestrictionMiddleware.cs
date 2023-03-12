using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Middleware;

/// <summary>Http Middleware that constraints access to specific Client Ips depending on request path rules.</summary>
public class ClientIpRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClientIpRestrictionMiddleware> _logger;
    private readonly ClientIpRestrictionOptions _options;

    /// <summary>construct the middleware</summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ClientIpRestrictionMiddleware(
        RequestDelegate next,
        ClientIpRestrictionOptions options,
        ILogger<ClientIpRestrictionMiddleware> logger) {
        
        _next = next;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Invokes the middleware.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public async Task Invoke(HttpContext httpContext) {
        if (!_options.Disabled && _options.TryMatch(httpContext, out var ipSafeList)) {
            var remoteIp = httpContext.Connection.RemoteIpAddress;
            _logger.LogDebug("Request from Remote IP address: {RemoteIp}", remoteIp);

            var bytes = remoteIp.GetAddressBytes();
            var badIp = true;
            foreach (var address in ipSafeList) {
                if (address.SequenceEqual(bytes)) {
                    badIp = false;
                    break;
                }
            }

            if (badIp) {
                _logger.LogWarning(
                    "Forbidden Request from Remote IP address: {RemoteIp}", remoteIp);
                httpContext.Response.StatusCode = (int)_options.HttpStatusCode;
                return;
            }
        }
        await _next?.Invoke(httpContext);
    }
}
