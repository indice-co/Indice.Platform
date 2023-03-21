using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.EmbeddedUI;

internal class StaticFileMiddlewareFactory
{
    private static readonly ConcurrentDictionary<string, StaticFileMiddleware> _cache = new();
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly ILoggerFactory _loggerFactory;

    public StaticFileMiddlewareFactory(IWebHostEnvironment hostingEnvironment, ILoggerFactory loggerFactory) {
        _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public StaticFileMiddleware Create(
        string requestPath,
        RequestDelegate next,
        Assembly assembly,
        string embeddedUIRoot,
        SpaUIOptions options,
        IContentTypeProvider contentTypeProvider = null
    ) {
        var hasCachedEntry = _cache.TryGetValue(requestPath, out var cachedEntry);
        if (hasCachedEntry) {
            return cachedEntry;
        }
        var baseNamespace = $"{assembly.GetName().Name}.{embeddedUIRoot.Replace("-", "_")}";
        var staticFileOptions = new StaticFileOptions {
            RequestPath = requestPath == "/" ? null : requestPath,
            FileProvider = new SpaFileProvider(new EmbeddedFileProvider(assembly, baseNamespace), options),
            ContentTypeProvider = contentTypeProvider ?? new FileExtensionContentTypeProvider()
        };
        if (options.OnPrepareResponse != null) {
            staticFileOptions.OnPrepareResponse = options.OnPrepareResponse;
        }
        var middleware = new StaticFileMiddleware(next, _hostingEnvironment, Options.Create(staticFileOptions), _loggerFactory);
        var _ = _cache.TryAdd(requestPath, middleware);
        return middleware;
    }
}
