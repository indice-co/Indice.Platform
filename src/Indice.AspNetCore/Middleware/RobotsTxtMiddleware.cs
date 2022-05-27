using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Middleware
{
    /// <summary>
    /// A middleware that allows to load the conrrect <em>robots.txt</em> from disk depending on the <see cref="IWebHostEnvironment"/>.<br/>
    /// The format is according to the <seealso cref="IHostEnvironment.EnvironmentName"/> (ie. <b>robots.Production.txt</b>). <br/>
    /// If not specific file is found it defaults to plain old <em>robots.txt</em>.
    /// </summary>
    /// <remarks>this code is taken from https://khalidabuhakmeh.com/robotstxt-middleware-aspnet-core</remarks>
    public class RobotsTxtMiddleware
    {
        const string Default =
            @"User-Agent: *\nAllow: /";

        private readonly RequestDelegate next;
        private readonly string environmentName;
        private readonly string rootPath;

        /// <summary>
        /// Constructs the <see cref="RobotsTxtMiddleware"/>
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="environmentName">In order to discover the correct robots.txt version to pick up the <paramref name="environmentName"/> is used to search for a pesific file on disc. For example in <b>Test</b> it wil search for <b>robots.Test.txt</b></param>
        /// <param name="rootPath">Override the directory to search for the <em>robots.txt</em> file. Defaults to <seealso cref="IHostEnvironment.ContentRootPath"/></param>
        public RobotsTxtMiddleware(
            RequestDelegate next,
            string environmentName,
            string rootPath
        ) {
            this.next = next;
            this.environmentName = environmentName;
            this.rootPath = rootPath;
        }

        /// <summary>
        /// Invokes the middleware and tries to find the correct file to serve.
        /// </summary>
        /// <param name="context">The http context</param>
        public async Task InvokeAsync(HttpContext context) {
            if (context.Request.Path.StartsWithSegments("/robots.txt")) {
                var generalRobotsTxt = Path.Combine(rootPath, "robots.txt");
                var environmentRobotsTxt = Path.Combine(rootPath, $"robots.{environmentName}.txt");
                string output;

                // try environment first
                if (File.Exists(environmentRobotsTxt)) {
                    output = await File.ReadAllTextAsync(environmentRobotsTxt);
                }
                // then robots.txt
                else if (File.Exists(generalRobotsTxt)) {
                    output = await File.ReadAllTextAsync(generalRobotsTxt);
                }
                // then just a general default
                else {
                    output = Default;
                }

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(output);
            } else {
                await next(context);
            }
        }
    }
}
