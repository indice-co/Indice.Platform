using System.Net;
using Microsoft.AspNetCore.Http;

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
                context.Connection.RemoteIpAddress = ipAddress;
            }
        }
        return _next(context);
    }
}

/// <summary></summary>
public class IpOverrideMiddlewareOptions 
{
    /// <summary></summary>
    public string IpAddress { get; set; }
}