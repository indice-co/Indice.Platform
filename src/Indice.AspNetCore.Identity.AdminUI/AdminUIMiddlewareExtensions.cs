using System;
using Indice.AspNetCore.EmbeddedUI;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware{TOptions}"/> middleware.
    /// </summary>
    /// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-5.0#middleware-extension-method</example>
    public static class AdminUIMiddlewareExtensions
    {
        /// <summary>
        /// Registers the Identity Server's Admin UI single page application, using the provided options.
        /// </summary>
        /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
        public static IApplicationBuilder UseAdminUI(this IApplicationBuilder builder, Action<SpaUIOptions> optionsAction = null) =>
            builder.UseSpaUI("admin-ui-dist", typeof(AdminUIMiddlewareExtensions).Assembly, optionsAction);
    }
}
