using System.Net.Mime;
using Indice.AspNetCore.Middleware;
using Microsoft.Extensions.Logging;
using static Indice.AspNetCore.Middleware.RequestResponseLoggingMiddleware;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods related to <see cref="IApplicationBuilder"/>.</summary>
public static class RequestResponseLoggingBuilderExtensions
{
    /// <summary>Adds Request &amp; Response Logging feature to the <see cref="IApplicationBuilder"/> request execution pipeline. Must be placed early on in the pipeline in order to catch the raw payload.</summary>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="contentTypes">These are the response content types that will be tracked. Others are filtered out. Whern null or empty array uses defaults application/json and text/html</param>
    /// <param name="logHandler">Pass a hander that will be used instead of the default internal logging.</param>
    /// <returns>The builder.</returns>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder, IEnumerable<string> contentTypes = null, Func<ILogger, RequestProfilerModel, Task> logHandler = null) =>
        builder.UseRequestResponseLogging((options) => {
            options.ContentTypes = contentTypes?.Count() > 0 ? contentTypes.ToList() : new List<string> { MediaTypeNames.Application.Json, MediaTypeNames.Text.Html };
            options.LogHandler = logHandler ?? DefaultLoggingHandler;
        });

    /// <summary>Adds Request &amp; Response Logging feature to the <see cref="IApplicationBuilder"/> request execution pipeline. Must be placed early on in the pipeline in order to catch the raw payload.</summary>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="configureAction">Action to configure available <see cref="RequestResponseLoggingOptions"/></param>
    /// <returns>The builder.</returns>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder, Action<RequestResponseLoggingOptions> configureAction) {
        var options = new RequestResponseLoggingOptions {
            ContentTypes = {
                MediaTypeNames.Application.Json,
                MediaTypeNames.Text.Html
            }
        };
        configureAction?.Invoke(options);
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>(options);
    }

}
