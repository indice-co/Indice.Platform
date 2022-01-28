using System;
using System.Reflection;
using Indice.AspNetCore.EmbeddedUI;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware"/> middleware.
    /// </summary>
    /// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-5.0#middleware-extension-method</example>
    public static class SpaUIMiddlewareExtensions
    {
        /// <summary>
        /// Registers the single page application, using the provided options.
        /// </summary>
        /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware"/> middleware.</param>
        /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
        /// <param name="assembly">The assembly containing the embedded resources.</param>
        public static IApplicationBuilder UseSpaUI(this IApplicationBuilder builder, SpaUIOptions options, string embeddedUIRoot, Assembly assembly) {
            if (options.Enabled) {
                options.Version = assembly.GetName().Version.ToString(fieldCount: 3);
                builder.UseMiddleware<SpaUIMiddleware>(options, embeddedUIRoot, assembly);
            }
            return builder;
        }

        /// <summary>
        /// Registers the single page application, using the provided options.
        /// </summary>
        /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
        /// <param name="assembly">The assembly containing the embedded resources.</param>
        /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware"/> middleware.</param>
        public static IApplicationBuilder UseSpaUI(this IApplicationBuilder builder, string embeddedUIRoot = "spa-ui-dist", Assembly assembly = null, Action<SpaUIOptions> optionsAction = null) {
            var options = new SpaUIOptions {
                Version = assembly.GetName().Version.ToString(fieldCount: 3)
            };
            optionsAction?.Invoke(options);
            return builder.UseSpaUI(options, embeddedUIRoot, assembly ?? Assembly.GetCallingAssembly());
        }
    }
}
