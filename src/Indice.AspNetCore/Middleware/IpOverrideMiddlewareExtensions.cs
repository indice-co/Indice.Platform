﻿using Indice.AspNetCore.Middleware;

namespace Microsoft.AspNetCore.Builder;

/// <summary></summary>
public static class IpOverrideMiddlewareExtensions
{
    private const string IpOverrideMiddlewareAdded = "IpOverrideMiddlewareAdded";

    /// <summary></summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IApplicationBuilder UseIpOverride(this IApplicationBuilder builder, Action<IpOverrideMiddlewareOptions> config) {
        if (builder == null) {
            throw new ArgumentNullException(nameof(builder));
        }
        if (!builder.Properties.ContainsKey(IpOverrideMiddlewareAdded)) {
            builder.Properties[IpOverrideMiddlewareAdded] = true;
            var options = new IpOverrideMiddlewareOptions();
            config?.Invoke(options);
            return builder.UseMiddleware<IpOverrideMiddleware>(options);
        }
        return builder;
    }
}