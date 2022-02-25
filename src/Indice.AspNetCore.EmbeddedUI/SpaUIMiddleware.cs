using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.EmbeddedUI
{
    /// <summary>
    /// A middleware used to configure and serve the admin back-office application for an Identity Server instance.
    /// </summary>
    public class SpaUIMiddleware
    {
        private readonly SpaUIOptions _options;
        private readonly RequestDelegate _next;
        private readonly StaticFileMiddleware _staticFileMiddleware;
        private StaticFileOptions _staticFileOptions;

        /// <summary>
        /// Constructs a new instance of <see cref="SpaUIMiddleware"/>.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware"/> middleware.</param>
        /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
        /// <param name="assembly">The assembly containing the embedded resources.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="next">A function that can process an HTTP request.</param>
        public SpaUIMiddleware(SpaUIOptions options, string embeddedUIRoot, Assembly assembly, ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment, RequestDelegate next) {
            _options = options ?? new SpaUIOptions();
            _next = next;
            _staticFileMiddleware = CreateStaticFileMiddleware(hostingEnvironment, loggerFactory, embeddedUIRoot, assembly);
        }

        /// <summary>
        /// Invokes the <see cref="SpaUIMiddleware"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public async Task Invoke(HttpContext httpContext) {
            var requestMethod = httpContext.Request.Method;
            var requestPath = httpContext.Request.Path.Value;
            if (_next != null && !requestPath.StartsWith(_options.Path, StringComparison.OrdinalIgnoreCase)) {
                await _next.Invoke(httpContext);
                return;
            }
            if (!_staticFileOptions.ContentTypeProvider.TryGetContentType(requestPath, out var _)) {
                httpContext.Request.Path = new PathString($"{_options.Path}/index.html");
            }
            await _staticFileMiddleware.Invoke(httpContext);
        }

        /// <summary>
        /// Configures a <see cref="StaticFileMiddleware"/> in order to serve the back-office application files from embedded resources.
        /// </summary>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
        /// <param name="assembly">The assembly containing the assets to pass to the internal <see cref="EmbeddedFileProvider"/>.</param>
        private StaticFileMiddleware CreateStaticFileMiddleware(IWebHostEnvironment hostingEnvironment, ILoggerFactory loggerFactory, string embeddedUIRoot, Assembly assembly) {
            var baseNamespace = $"{assembly.GetName().Name}.{embeddedUIRoot.Replace("-", "_")}";
            _staticFileOptions = new StaticFileOptions {
                RequestPath = string.IsNullOrEmpty(_options.Path) ? string.Empty : _options.Path,
                FileProvider = new SpaFileProvider(new EmbeddedFileProvider(assembly, baseNamespace), _options),
                ContentTypeProvider = new FileExtensionContentTypeProvider()
            };
            if (_options.OnPrepareResponse != null) {
                _staticFileOptions.OnPrepareResponse = _options.OnPrepareResponse;
            }
            return new StaticFileMiddleware(_next, hostingEnvironment, Options.Create(_staticFileOptions), loggerFactory);
        }
    }
}
