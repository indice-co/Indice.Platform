using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Indice.Extensions;
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
        private static readonly string Namespace = typeof(AdminUIMiddleware).Namespace;
        private const string SpaDistFolder = "admin_ui_dist"; // Used in snake case as it used when refering to an embedded resource.
        private readonly AdminUIOptions _options;
        private readonly RequestDelegate _next;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly StaticFileMiddleware _staticFileMiddleware;
        private StaticFileOptions _staticFileOptions;

        /// <summary>
        /// Constructs a new instance of <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="AdminUIMiddleware"/> middleware.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="next">A function that can process an HTTP request.</param>
        /// <param name="httpContextAccessor">Used to access the <see cref="HttpContext"/> through the <see cref="IHttpContextAccessor"/> interface and its default implementation <see cref="HttpContextAccessor"/>.</param>
        public AdminUIMiddleware(
            AdminUIOptions options,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment hostingEnvironment,
            RequestDelegate next,
            IHttpContextAccessor httpContextAccessor
        ) {
            _next = next;
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _options = options ?? new AdminUIOptions();
            _staticFileMiddleware = CreateStaticFileMiddleware(hostingEnvironment, loggerFactory, options);
        }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> ResourcesFileNamePaths => new HashSet<string>(CreateResourcesFileNamePaths());

        /// <summary>
        /// Invokes the <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public async Task Invoke(HttpContext httpContext) {
            var requestMethod = httpContext.Request.Method;
            var requestPath = httpContext.Request.Path.Value;
            var resource = requestPath.Replace($"/{_options.Path}", string.Empty);
            if (ResourcesFileNamePaths.Contains(resource)) {
                await RespondWithResource(httpContext.Response, resource);
                return;
            }
            // If method is of type GET and path is the configured one, then handle the request and respond with the SPA's index.html page.
            if (requestMethod == HttpMethod.Get.Method && Regex.IsMatch(requestPath, $"^/?{Regex.Escape(_options.Path)}/?(?!.*\\.(js|css|jpg|woff|woff2|svg|ttf|eot|png)($|\\?)).*", RegexOptions.IgnoreCase)) {
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
                FileProvider = new ManifestEmbeddedFileProvider(typeof(AdminUIMiddleware).GetTypeInfo().Assembly),
                OnPrepareResponse = options.OnPrepareResponse
            };
            return new StaticFileMiddleware(_next, hostingEnvironment, Options.Create(_staticFileOptions), loggerFactory);
        }

        private async Task RespondWithIndexHtml(HttpResponse response) {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = $"{MediaTypeNames.Text.Html};charset=utf-8";
            using (var stream = GetPageStream("index.html")) {
                using (var streamReader = new StreamReader(stream)) {
                    var htmlBuilder = new StringBuilder(streamReader.ReadToEnd());
                    foreach (var argument in GetIndexArguments()) {
                        htmlBuilder.Replace(argument.Key, argument.Value);
                    }
                    await response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                }
            }
        }

        private async Task RespondWithResource(HttpResponse response, string fileName) {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = FileExtensions.GetMimeType(Path.GetExtension(fileName));
            using (var stream = GetPageStream(fileName)) {
                //_options.OnPrepareResponse?.Invoke(new StaticFileResponseContext(_httpContextAccessor.HttpContext, _staticFileOptions.FileProvider.GetFileInfo(fileName)));
                await stream.CopyToAsync(response.Body);
            }
        }

        private Stream GetPageStream(string fileName) => typeof(AdminUIMiddleware).GetTypeInfo().Assembly.GetManifestResourceStream($"{Namespace}.{SpaDistFolder}.{fileName.Trim('/').Replace('/', '.')}");

        private IDictionary<string, string> GetIndexArguments() => new Dictionary<string, string>() {
            { "%(Authority)", _options.Authority },
            { "%(ClientId)", _options.ClientId },
            { "%(DocumentTitle)", _options.DocumentTitle },
            { "%(Host)", _options.Host },
            { "%(Path)", _options.Path }
        };

        private string[] CreateResourcesFileNamePaths() {
            var result = new List<string>();
            var resources = typeof(AdminUIMiddleware).GetTypeInfo().Assembly.GetManifestResourceNames();
            foreach (var name in resources) {
                var nameParts = name.Split('.').Skip(5).ToArray(); // Skip first 2 parts which are namespace and spa dist folder name.
                if (nameParts.Last() == "xml") { // Exclude manifest embedded file provider xml.
                    continue;
                }
                var length = nameParts.Length;
                var fileName = $"{nameParts[length - 2]}.{nameParts[length - 1]}";
                var path = string.Empty;
                for (var i = 0; i < length - 2; i++) {
                    path += $"/{nameParts[i]}";
                }
                result.Add($"{path}/{fileName}");
            }
            return result.ToArray();
        }
    }
}
