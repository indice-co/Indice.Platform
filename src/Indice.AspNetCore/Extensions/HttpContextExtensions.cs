using System.Net;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Extensions;

/// <summary>Extension methods on <see cref="HttpContext"/>.</summary>
public static class HttpContextExtensions
{
    /// <summary>Tries to identify the IP address of the request.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static IPAddress GetClientIpAddress(this HttpContext httpContext) {
        var headers = httpContext.Request.Headers;
        string ipAddress = null;
        // in case of a proxy being used, the UseForwardedHeaders() middleware should be configured to the HTTP request pipeline.
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIpAddress)) {
            ipAddress = remoteIpAddress;
        }
        if (headers.TryGetValue("REMOTE_ADDR", out var remoteAddress)) {
            ipAddress = remoteAddress;
        }
        return ipAddress is not null ? IPAddress.Parse(ipAddress) : IPAddress.None;
    }
}