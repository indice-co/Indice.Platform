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
        private ILogger<AdminUIMiddleware> _logger;

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
            _logger = loggerFactory.CreateLogger<AdminUIMiddleware>();
        }

        /// <summary>
        /// Invokes the <see cref="AdminUIMiddleware"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public async Task Invoke(HttpContext httpContext) {
            var requestMethod = httpContext.Request.Method;
            var requestPath = httpContext.Request.Path.Value;
            if (_next != null && !requestPath.StartsWith(_options.Path, StringComparison.OrdinalIgnoreCase)) {
                await _next.Invoke(httpContext);
                return;
            }
            if (requestPath.StartsWith(_options.Path, StringComparison.OrdinalIgnoreCase) && !_staticFileOptions.ContentTypeProvider.TryGetContentType(requestPath, out var _)) {
                httpContext.Request.Path = new PathString($"{_options.Path}/index.html");
            }
            await _staticFileMiddleware.Invoke(httpContext);
        }

        /// <summary>
        /// Configures a <see cref="StaticFileMiddleware"/> in order to serve the back-office application files from embedded resources.
        /// </summary>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="options">Options for configuring <see cref="AdminUIMiddleware"/> middleware.</param>
        private StaticFileMiddleware CreateStaticFileMiddleware(IWebHostEnvironment hostingEnvironment, ILoggerFactory loggerFactory, AdminUIOptions options) {
            _staticFileOptions = new StaticFileOptions {
                RequestPath = string.IsNullOrEmpty(options.Path) ? string.Empty : options.Path,
                FileProvider = new SpaFileProvider(new EmbeddedFileProvider(typeof(AdminUIMiddleware).GetTypeInfo().Assembly, EmbeddedFilesNamespace), _options),
                ContentTypeProvider = new FileExtensionContentTypeProvider()
            };
            if (options.OnPrepareResponse != null) {
                _staticFileOptions.OnPrepareResponse = options.OnPrepareResponse;
            }
            return new StaticFileMiddleware(_next, hostingEnvironment, Options.Create(_staticFileOptions), loggerFactory);
        }
    }
}
