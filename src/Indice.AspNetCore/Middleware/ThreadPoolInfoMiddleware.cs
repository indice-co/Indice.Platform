using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Middleware;

/// <summary>Extension method to register an endpoint that debugs basic thread pool info.</summary>
public static class ThreadPoolInfoMiddlewareExtensions
{
    /// <summary>Registers an endpoint that debugs basic thread pool info.</summary>
    /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <param name="pattern">The route pattern i.e. /thread-pool-info</param>
    public static IEndpointConventionBuilder MapThreadPoolInfo(this IEndpointRouteBuilder endpoints, string pattern) {
        var app = endpoints.CreateApplicationBuilder();
        app.Use(new Func<HttpContext, RequestDelegate, Task>(async (httpContext, next) => {
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableIOThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIO);
            await httpContext.Response.WriteAsync($"Available Worker Threads: {availableWorkerThreads}\r\n");
            await httpContext.Response.WriteAsync($"Available Asynchronous I/O Threads: {availableIOThreads}\r\n");
            await httpContext.Response.WriteAsync($"Max Worker Threads: {maxWorkerThreads}\r\n");
            await httpContext.Response.WriteAsync($"Max Asynchronous I/O Threads: {maxIO}\r\n");
        }));
        var pipeline = app.Build();
        return endpoints.MapGet(pattern, pipeline);
    }
}