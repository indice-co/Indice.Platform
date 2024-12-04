#nullable enable
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;

namespace Indice.AspNetCore.Middleware;

/// <summary>
/// Extension methods to register that allow to debug running behind a reverse proxy.
/// Will register a GET endpoint that renders plain text to the response will all incoming headers
/// </summary>
public static class ProxyDebugExtensions
{
    /// <summary>Debug proxy endpoint configuration options.</summary>
    public class DebugProxyOptions
    {
        /// <summary>Controls whether the endpoint will also register its own instance of <see cref="ForwardedHeadersMiddleware"/> and <seealso cref="HttpMethodOverrideMiddleware"/>. Defaults to true.</summary>
        /// <remarks>It is expected that we set this to false in case we got <see cref="ForwardedHeadersMiddleware"/> already configured earlier in the pipeline.</remarks>
        public bool UseOwnMiddleware { get; set; } = true;
    }

    /// <summary>Registers an endpoint that allows to debug running behind a reverse proxy. Will register a GET endpoint that renders plain text to the response will all incoming headers.</summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to extend</param>
    /// <param name="pattern">The route pattern ie /proxy-debug.</param>
    /// <param name="configure">Configure action.</param>
    /// <remarks>Defaults to endpoint path <strong>/proxy-debug</strong></remarks>
    /// <returns></returns>
    public static IEndpointConventionBuilder MapProxyDebug(this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern = "/proxy-debug",
        Action<DebugProxyOptions>? configure = null) {
        var options = new DebugProxyOptions();
        configure?.Invoke(options);
        var app = endpoints.CreateApplicationBuilder();
        if (options.UseOwnMiddleware) {
            app.UseForwardedHeaders(new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.All
            })
            .UseHttpMethodOverride();
        }
        app.Use(new Func<HttpContext, RequestDelegate, Task>(async (context, next) => {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status200OK;
            foreach (var header in context.Request.Headers) {
                await context.Response.WriteAsync($"{header.Key}: {header.Value}\r\n");
            }
            await context.Response.WriteAsync($"Method: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"Scheme: {context.Request.Scheme}\r\n");
            await context.Response.WriteAsync($"RemoteIP: {context.Connection.RemoteIpAddress}\r\n");
            await context.Response.WriteAsync($"RemotePort: {context.Connection.RemotePort}\r\n");
        }));
        var pipeline = app.Build();
        return endpoints.MapGet(pattern, pipeline);
    }
}
#nullable disable