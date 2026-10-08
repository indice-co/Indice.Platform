using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Indice.AspNetCore.Middleware;

/// <summary>A middleware used to debug access from an arbitrary IP</summary>
/// <remarks>This should only be used in test environments to debug the access from various client ips. A test scenario would be impossible travel detection.</remarks>
public class IpOverrideMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpOverrideMiddlewareOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpOverrideMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    /// <param name="options">The configuration options for IP address overriding.</param>
    public IpOverrideMiddleware(RequestDelegate next, IpOverrideMiddlewareOptions options) {
        _next = next;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes the current HTTP request, overriding the client IP address when configured.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public Task Invoke(HttpContext context) {
        var ipAddressOption = _options.IpAddress;
        if (!string.IsNullOrWhiteSpace(ipAddressOption)) {
            var isValidIp = IPAddress.TryParse(ipAddressOption, out var ipAddress);
            if (isValidIp) {
                if (_options.UseForwardedFor && context.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor)) {
                    var ips = xForwardedFor.ToArray();
                    ips[0] = ipAddress!.ToString();
                    context.Request.Headers["X-Forwarded-For"] = new StringValues(ips);
                } else {
                    context.Connection.RemoteIpAddress = ipAddress;
                }
            }
        }
        return _next(context);
    }
}

/// <summary>Options for the configuring the <see cref="IpOverrideMiddleware"/></summary>
public class IpOverrideMiddlewareOptions
{
    /// <summary>The client IP address that is impersonated</summary>
    public string IpAddress { get; set; } = null!;
    /// <summary>
    /// True, when behind proxy
    /// </summary>
    public bool UseForwardedFor { get; set; }
}