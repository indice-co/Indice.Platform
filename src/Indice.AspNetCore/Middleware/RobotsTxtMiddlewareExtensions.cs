using Indice.AspNetCore.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods On the <see cref="IApplicationBuilder"/>
    /// </summary>
    /// <remarks>this code is taken from https://khalidabuhakmeh.com/robotstxt-middleware-aspnet-core</remarks>
    public static class RobotsTxtMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="RobotsTxtMiddleware"/> to the pipeline
        /// </summary>
        /// <param name="builder">The application pipeline builder</param>
        /// <param name="env">The <see cref="IWebHostEnvironment"/> is used to discover the correct robots.txt version to pick up depending on the environment name. For example in <b>Test</b> it wil search for <b>robots.Test.txt</b></param>
        /// <param name="rootPath">Override the directory to search for the <em>robots.txt</em> file. Defaults to <seealso cref="IHostEnvironment.ContentRootPath"/></param>
        /// <returns>The input builder for further configuration</returns>
        public static IApplicationBuilder UseRobotsTxt(
            this IApplicationBuilder builder,
            IWebHostEnvironment env,
            string rootPath = null
        ) {
            return builder.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/robots.txt"), b =>
                b.UseMiddleware<RobotsTxtMiddleware>(env.EnvironmentName, rootPath ?? env.ContentRootPath));
        }
    }

}
