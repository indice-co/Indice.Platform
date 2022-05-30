using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Middleware
{
    /// <summary>
    /// A middleware that allows to load the correct <em>robots.txt</em> from disk depending on the <see cref="IWebHostEnvironment"/>.<br/>
    /// The format is according to the <seealso cref="IHostEnvironment.EnvironmentName"/> (i.e. <b>robots.Production.txt</b>).<br/>
    /// If not specific file is found it defaults to plain old <em>robots.txt</em>.
    /// </summary>
    /// <remarks>This code is taken from https://khalidabuhakmeh.com/robotstxt-middleware-aspnet-core</remarks>.
    public class RobotsTxtMiddleware
    {
        const string Default = @"User-Agent: *\nAllow: /";
        private readonly RequestDelegate _next;
        private readonly string _environmentName;
        private readonly string _rootPath;

        /// <summary>Constructs the <see cref="RobotsTxtMiddleware"/>.</summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="environmentName">In order to discover the correct robots.txt version to pick up the <paramref name="environmentName"/> is used to search for a specific file on disc. For example in <b>Test</b> it will search for <b>robots.Test.txt</b>.</param>
        /// <param name="rootPath">Override the directory to search for the <em>robots.txt</em> file. Defaults to <seealso cref="IHostEnvironment.ContentRootPath"/>.</param>
        public RobotsTxtMiddleware(RequestDelegate next, string environmentName, string rootPath) {
            _next = next;
            _environmentName = environmentName;
            _rootPath = rootPath;
        }

        /// <summary>Invokes the middleware and tries to find the correct file to serve.</summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context) {
            if (context.Request.Path.StartsWithSegments("/robots.txt")) {
                var generalRobotsTxt = Path.Combine(_rootPath, "robots.txt");
                var environmentRobotsTxt = Path.Combine(_rootPath, $"robots.{_environmentName}.txt");
                string output;
                // Try environment first.
                if (File.Exists(environmentRobotsTxt)) {
                    output = await File.ReadAllTextAsync(environmentRobotsTxt);
                }
                // Then robots.txt
                else if (File.Exists(generalRobotsTxt)) {
                    output = await File.ReadAllTextAsync(generalRobotsTxt);
                }
                // Then just a general default.
                else {
                    output = Default;
                }
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(output);
            } else {
                await _next(context);
            }
        }
    }
}
