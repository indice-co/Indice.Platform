using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Extensions;

/// <summary>Extension methods on <see cref="HttpContext"/>.</summary>
public static class HttpContextExtensions
{
    /// <summary>Tries to identify the IP address of the request.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static string GetClientIpAddress(this HttpContext httpContext) {
        var headers = httpContext.Request.Headers;
        if (headers.TryGetValue("X-Forwarded-For", out var xForwardedFor)) {
            return xForwardedFor;
        }
        var ipAddress = httpContext.Connection.RemoteIpAddress.ToString();
        if (!string.IsNullOrWhiteSpace(ipAddress)) {
            return ipAddress;
        }
        if (headers.TryGetValue("REMOTE_ADDR", out var remoteAddress)) {
            return remoteAddress;
        }
        return null;
    }
}
