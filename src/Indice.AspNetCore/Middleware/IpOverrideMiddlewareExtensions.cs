using Indice.AspNetCore.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods to register and configure the <see cref="IpOverrideMiddleware"/></summary>
public static class IpOverrideMiddlewareExtensions
{
    private const string IpOverrideMiddlewareAdded = "IpOverrideMiddlewareAdded";

    /// <summary>Adds a middleware that will overrite the incomming ip of the real client so that we can debug with a fixed ip origin.</summary>
    /// <remarks>Not intended to be used in production</remarks>
    /// <param name="builder">The application builder</param>
    /// <param name="config">The configure action</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IApplicationBuilder UseIpOverride(this IApplicationBuilder builder, Action<IpOverrideMiddlewareOptions> config) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(config);
        if (!builder.Properties.ContainsKey(IpOverrideMiddlewareAdded)) {
            builder.Properties[IpOverrideMiddlewareAdded] = true;
            var options = new IpOverrideMiddlewareOptions();
            var useProxy = builder.ApplicationServices.GetRequiredService<IConfiguration>().GetValue<bool>($"Proxy:Enabled");
            if (useProxy) {
                options.UseForwardedFor = true;
            }
            config?.Invoke(options);
            return builder.UseMiddleware<IpOverrideMiddleware>(options);
        }
        return builder;
    }
}