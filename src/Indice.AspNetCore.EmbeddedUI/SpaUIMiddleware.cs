using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.EmbeddedUI
{
    /// <summary>A middleware used to configure and serve a back-office application for an API instance.</summary>
    public class SpaUIMiddleware<TOptions> where TOptions : SpaUIOptions, new()
    {
        private readonly TOptions _options;
        private readonly string _embeddedUIRoot;
        private readonly Assembly _assembly;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly RequestDelegate _next;

        /// <summary>Constructs a new instance of <see cref="SpaUIMiddleware{TOptions}"/>.</summary>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
        /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
        /// <param name="assembly">The assembly containing the embedded resources.</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="next">A function that can process an HTTP request.</param>
        public SpaUIMiddleware(
            TOptions options,
            string embeddedUIRoot,
            Assembly assembly,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment hostingEnvironment,
            RequestDelegate next
        ) {
            _options = options ?? new TOptions();
            _embeddedUIRoot = embeddedUIRoot ?? throw new ArgumentNullException(nameof(embeddedUIRoot));
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>Invokes the <see cref="SpaUIMiddleware{TOptions}"/>.</summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public async Task Invoke(HttpContext httpContext) {
            var routeMatcher = new RouteMatcher(httpContext);
            var isMatch = routeMatcher.IsMatch(_options.PathPrefixPattern, out var resolvedParameters);
            if (_next is not null && !isMatch) {
                await _next.Invoke(httpContext);
                return;
            }
            var baseRequestPath = _options.PathPrefixPattern.GetRequestPath(resolvedParameters);
            _options.Path = baseRequestPath;
            if (_options.Multitenancy) {
                var tenantId = _options.TenantIdAccessor(httpContext, resolvedParameters);
                _options.TenantId = tenantId;
            }
            var staticFileMiddlewareFactory = new StaticFileMiddlewareFactory(_hostingEnvironment, _loggerFactory);
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            var staticFileMiddleware = staticFileMiddlewareFactory.Create(baseRequestPath, _next, _assembly, _embeddedUIRoot, _options, contentTypeProvider);
            var requestPath = httpContext.Request.Path.Value;
            if (!contentTypeProvider.TryGetContentType(requestPath, out var _)) {
                httpContext.Request.Path = new PathString($"{baseRequestPath.TrimEnd('/')}/index.html");
            }
            await staticFileMiddleware.Invoke(httpContext);
        }
    }
}
