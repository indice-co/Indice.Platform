using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Indice.AspNetCore.Middleware;

/// <summary></summary>
public class IpOverrideMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpOverrideMiddlewareOptions _options;

    /// <summary></summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    public IpOverrideMiddleware(RequestDelegate next, IpOverrideMiddlewareOptions options) {
        _next = next;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary></summary>
    /// <param name="context"></param>
    public Task Invoke(HttpContext context) {
        var ipAddressOption = _options.IpAddress;
        if (!string.IsNullOrWhiteSpace(ipAddressOption)) {
            var isValidIp = IPAddress.TryParse(ipAddressOption, out var ipAddress);
            if (isValidIp) {
                if (_options.UseForwardedFor && context.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor)) {
                    var ips = xForwardedFor.ToArray();
                    ips[0] = ipAddress.ToString();
                    context.Request.Headers["X-Forwarded-For"] = new StringValues(ips);
                } else {
                    context.Connection.RemoteIpAddress = ipAddress;
                }
            }
        }
        return _next(context);
    }
}

/// <summary></summary>
public class IpOverrideMiddlewareOptions
{
    /// <summary>The client IP address that is impersonated</summary>
    public string IpAddress { get; set; }
    /// <summary>
    /// True, when behind proxy
    /// </summary>
    public bool UseForwardedFor { get; set; }
}