using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.AdminUI
{
    /// <summary>
    /// A middleware used to configure and serve the admin back-office application for an Identity Server instance.
    /// </summary>
    public class AdminUIMiddleware
    {
        private static readonly string EmbeddedFilesNamespace = $"{typeof(AdminUIMiddleware).Namespace}.admin_ui_dist";
        private readonly AdminUIOptions _options;
        private readonly RequestDelegate _next;
        private readonly StaticFileMiddleware _staticFileMiddleware;
        private StaticFileOptions _staticFileOptions;

        /// <summary>
        /// Constructs a new instance of <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="AdminUIMiddleware"/> middleware.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="next">A function that can process an HTTP request.</param>
        public AdminUIMiddleware(
            AdminUIOptions options,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment hostingEnvironment,
            RequestDelegate next
        ) {
            _options = options ?? new AdminUIOptions();
            _next = next;
            _staticFileMiddleware = CreateStaticFileMiddleware(hostingEnvironment, loggerFactory, options);
        }

        /// <summary>
        /// Invokes the <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public async Task Invoke(HttpContext httpContext) {
            if (!_options.Enabled) {
                await NotFound(httpContext.Response);
            }
            var requestMethod = httpContext.Request.Method;
            var requestPath = httpContext.Request.Path.Value;
            // If method is of type GET and path is the configured one, then handle the request and respond with the SPA's index.html page.
            var shouldDisplayIndex = requestMethod == HttpMethod.Get.Method && Regex.IsMatch(requestPath, $"^/?{Regex.Escape(_options.Path)}/?(?!.*\\.(js|css|jpg|woff|woff2|svg|ttf|eot|png)($|\\?)).*", RegexOptions.IgnoreCase);
            if (shouldDisplayIndex) {
                await RespondWithIndexHtml(httpContext.Response);
                return;
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
            _staticFileOptions = new StaticFileOptions {
                RequestPath = string.IsNullOrEmpty(options.Path) ? string.Empty : $"/{options.Path}",
                FileProvider = new EmbeddedFileProvider(typeof(AdminUIMiddleware).GetTypeInfo().Assembly, EmbeddedFilesNamespace)
            };
            if (options.OnPrepareResponse != null) {
                _staticFileOptions.OnPrepareResponse = options.OnPrepareResponse;
            }
            return new StaticFileMiddleware(_next, hostingEnvironment, Options.Create(_staticFileOptions), loggerFactory);
        }

        private async Task RespondWithIndexHtml(HttpResponse response) {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = $"{MediaTypeNames.Text.Html};charset=utf-8";
            var indexFileInfo = _staticFileOptions.FileProvider.GetFileInfo("index.html");
            using (var stream = indexFileInfo.CreateReadStream()) {
                using (var streamReader = new StreamReader(stream)) {
                    var htmlBuilder = new StringBuilder(streamReader.ReadToEnd());
                    foreach (var argument in GetIndexArguments()) {
                        htmlBuilder.Replace(argument.Key, argument.Value);
                    }
                    await response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                }
            }
        }

        private async Task NotFound(HttpResponse response) {
            response.StatusCode = StatusCodes.Status404NotFound;
            response.Headers.Clear();
            await response.WriteAsync(string.Empty);
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
