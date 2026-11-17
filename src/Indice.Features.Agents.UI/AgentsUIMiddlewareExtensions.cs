using Indice.AspNetCore.EmbeddedUI;
using Indice.Features.Agents.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
/// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-5.0#middleware-extension-method</example>
public static class AgentsUIMiddlewareExtensions
{
    private const string IndexFileName = "index.html";
    private const string WidgetFileName = "widget.js";
    private const string SameOriginFrameAncestors = "frame-ancestors 'self'";
    /// <summary>The <see cref="HttpContext.Items"/> key holding the path as requested, before the SPA rewrites it.</summary>
    private static readonly object RequestedPathKey = new();

    /// <summary>Registers the Agents UI single page application, using the provided options.</summary>
    /// <remarks>
    /// Every page of the UI is sent with <c>Content-Security-Policy: frame-ancestors 'self'</c>, except the compact
    /// <c>/embed</c> route that the chat widget frames, which also allows <see cref="AgentsWidgetOptions.AllowedOrigins"/>.
    /// </remarks>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    /// <exception cref="InvalidOperationException">A widget allowed origin is not an exact origin.</exception>
    public static IApplicationBuilder UseAgentsUI(this IApplicationBuilder builder, Action<AgentsUIOptions>? optionsAction = null) {
        // SpaUIMiddleware rewrites every client route to {base}/index.html before the file is served, so the route that
        // was asked for (/embed or any other) has to be remembered here, ahead of it.
        builder.Use((context, next) => {
            context.Items[RequestedPathKey] = context.Request.Path;
            return next(context);
        });
        return builder.UseSpaUI<AgentsUIOptions>("browser", typeof(AgentsUIMiddlewareExtensions).Assembly, options => {
            optionsAction?.Invoke(options);
            options.Widget.Origins = options.Widget.GetNormalizedOrigins();
            var embedFrameAncestors = string.Join(' ', [SameOriginFrameAncestors, .. options.Widget.Origins]);
            var hostOnPrepareResponse = options.OnPrepareResponse;
            options.OnPrepareResponse = context => {
                ApplyWidgetHeaders(context, embedFrameAncestors);
                hostOnPrepareResponse?.Invoke(context);
            };
        });
    }

    /// <summary>Framing policy on the SPA page, and a short cache on the unhashed widget loader.</summary>
    private static void ApplyWidgetHeaders(StaticFileResponseContext context, string embedFrameAncestors) {
        var httpContext = context.Context;
        if (context.File.Name.Equals(IndexFileName, StringComparison.OrdinalIgnoreCase)) {
            var servedPath = httpContext.Request.Path.Value ?? string.Empty;
            var basePath = servedPath.EndsWith($"/{IndexFileName}", StringComparison.OrdinalIgnoreCase)
                ? servedPath[..^(IndexFileName.Length + 1)]
                : string.Empty;
            var requestedPath = httpContext.Items[RequestedPathKey] is PathString path ? path : httpContext.Request.Path;
            var isEmbed = requestedPath.StartsWithSegments(new PathString($"{basePath}/embed"), StringComparison.OrdinalIgnoreCase);
            httpContext.Response.Headers.Append(HeaderNames.ContentSecurityPolicy, isEmbed ? embedFrameAncestors : SameOriginFrameAncestors);
        } else if (context.File.Name.Equals(WidgetFileName, StringComparison.OrdinalIgnoreCase)) {
            // Customers paste a fixed, unversioned URL, so the loader must not be cached for long.
            httpContext.Response.Headers.CacheControl = "public, max-age=300";
        }
    }
}
