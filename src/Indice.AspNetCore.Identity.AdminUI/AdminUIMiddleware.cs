using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Identity.AdminUI
{
    /// <summary>
    /// A middleware used to configure and serve the admin back-office application for an Identity Server instance.
    /// </summary>
    public class AdminUIMiddleware
    {
        private readonly AdminUIOptions _options;
        private readonly RequestDelegate _next;
        private readonly StaticFileMiddleware _staticFileMiddleware;

        /// <summary>
        /// Constructs a new instance of <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="next">A function that can process an HTTP request.</param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        public AdminUIMiddleware(
            AdminUIOptions options,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment hostingEnvironment,
            RequestDelegate next
        ) {
            _next = next;
            _options = options ?? new AdminUIOptions();
            _staticFileMiddleware = CreateStaticFileMiddleware(hostingEnvironment, loggerFactory, options);
        }

        /// <summary>
        /// 
        /// </summary>
        public string[] ResourcesFileNames => typeof(AdminUIMiddleware).GetTypeInfo().Assembly.GetManifestResourceNames().Select(name => {
            var nameParts = name.Split('.');
            return string.Empty;
        })
        .ToArray();

        /// <summary>
        /// Invokes the <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public async Task Invoke(HttpContext httpContext) {
            var httpMethod = httpContext.Request.Method;
            var path = httpContext.Request.Path.Value;
            // If method is of type GET and path is the configured one, then handle the request and respond with the SPA's index.html page.
            if (httpMethod == HttpMethod.Get.Method && Regex.IsMatch(path, $"^/?{Regex.Escape(_options.Path)}/?$", RegexOptions.IgnoreCase)) {
                await RespondWithIndexHtml(httpContext.Response);
                return;
            }
            if (true) {

            }
            await _staticFileMiddleware.Invoke(httpContext);
        }

        /// <summary>
        /// Configures a <see cref="StaticFileMiddleware"/> in order to serve the back-office application files from embedded resources.
        /// </summary>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="options">Options for configuring <see cref="AdminUIMiddleware"/> middleware.</param>
        /// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers?view=aspnetcore-5.0#manifestembeddedfileprovider</example>
        private StaticFileMiddleware CreateStaticFileMiddleware(IWebHostEnvironment hostingEnvironment, ILoggerFactory loggerFactory, AdminUIOptions options) {
            var staticFileOptions = new StaticFileOptions {
                RequestPath = string.IsNullOrEmpty(options.Path) ? string.Empty : $"/{options.Path.TrimStart('/')}",
                FileProvider = new ManifestEmbeddedFileProvider(typeof(AdminUIMiddleware).GetTypeInfo().Assembly),
            };
            return new StaticFileMiddleware(_next, hostingEnvironment, Options.Create(staticFileOptions), loggerFactory);
        }

        private async Task RespondWithIndexHtml(HttpResponse response) {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = $"{MediaTypeNames.Text.Html};charset=utf-8";
            static Stream GetIndexPageStream() => typeof(AdminUIMiddleware).GetTypeInfo().Assembly.GetManifestResourceStream("Indice.AspNetCore.Identity.AdminUI.admin_ui.index.html");
            using (var stream = GetIndexPageStream()) {
                using (var streamReader = new StreamReader(stream)) {
                    var htmlBuilder = new StringBuilder(streamReader.ReadToEnd());
                    foreach (var argument in GetIndexArguments()) {
                        htmlBuilder.Replace(argument.Key, argument.Value);
                    }
                    await response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                }
            }
        }

        private IDictionary<string, string> GetIndexArguments() => new Dictionary<string, string>() {
            { "%(Authority)", _options.Authority },
            { "%(ClientId)", _options.ClientId },
            { "%(DocumentTitle)", _options.DocumentTitle },
            { "%(Host)", _options.Host },
            { "%(Path)", _options.Path }
        };
    }
}
