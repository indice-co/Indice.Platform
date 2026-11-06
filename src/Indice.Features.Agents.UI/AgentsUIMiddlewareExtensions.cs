using Indice.AspNetCore.EmbeddedUI;
using Indice.Features.Agents.UI;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
/// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-5.0#middleware-extension-method</example>
public static class AgentsUIMiddlewareExtensions
{
    /// <summary>Registers the Agents UI single page application, using the provided options.</summary>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    public static IApplicationBuilder UseAgentsUI(this IApplicationBuilder builder, Action<AgentsUIOptions>? optionsAction = null) =>
        builder.UseSpaUI<AgentsUIOptions>("browser", typeof(AgentsUIMiddlewareExtensions).Assembly, options => {
            optionsAction?.Invoke(options);
            ApplyFrameAncestors(options);
        });

    /// <summary>Sends the <see cref="AgentsUIOptions.FrameAncestors"/> policy with the app's page, ahead of any hook the host set.</summary>
    private static void ApplyFrameAncestors(AgentsUIOptions options) {
        var policy = string.Join(' ', ["frame-ancestors 'self'", .. options.FrameAncestors.Select(ToOrigin).Distinct()]);
        var hostHook = options.OnPrepareResponse;
        options.OnPrepareResponse = context => {
            // Only the HTML page is ever framed — scripts, styles and images need no policy.
            if (context.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase)) {
                context.Context.Response.Headers.ContentSecurityPolicy = policy;
            }
            hostHook?.Invoke(context);
        };
    }

    /// <summary>Reduces a configured origin to its scheme, host and port; anything else fails the startup rather than reach a response header.</summary>
    private static string ToOrigin(string value) {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) {
            throw new InvalidOperationException($"'{value}' is not a valid {nameof(AgentsUIOptions)}.{nameof(AgentsUIOptions.FrameAncestors)} origin. Use an absolute http(s) origin, e.g. https://portal.example.com.");
        }
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
