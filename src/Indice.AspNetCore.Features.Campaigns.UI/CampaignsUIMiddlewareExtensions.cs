using System;
using Indice.AspNetCore.EmbeddedUI;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware"/> middleware.
    /// </summary>
    /// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-5.0#middleware-extension-method</example>
    public static class CampaignsUIMiddlewareExtensions
    {
        /// <summary>
        /// Registers the Campaigns UI single page application, using the provided options.
        /// </summary>
        /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware"/> middleware.</param>
        public static IApplicationBuilder UseCampaignsUI(this IApplicationBuilder builder, SpaUIOptions options) =>
            builder.UseSpaUI(options, "campaigns-ui-dist", typeof(CampaignsUIMiddlewareExtensions).Assembly);

        /// <summary>
        /// Registers the Campaigns UI single page application, using the provided options.
        /// </summary>
        /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware"/> middleware.</param>
        public static IApplicationBuilder UseCampaignsUI(this IApplicationBuilder builder, Action<SpaUIOptions> optionsAction = null) =>
            builder.UseSpaUI("campaigns-ui-dist", typeof(CampaignsUIMiddlewareExtensions).Assembly, optionsAction);
    }
}
