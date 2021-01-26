using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Middleware;
using Microsoft.Extensions.Logging;
using static Indice.AspNetCore.Middleware.RequestResponseLoggingMiddleware;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods related to <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class RequestResponseLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds Request &amp; Response Logging feature to the <see cref="IApplicationBuilder"/> request execution pipeline. Must be placed early on in the pipeline in order to catch the raw payload.
        /// </summary>
        /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="contentTypes">These are the response content types that will be tracked. Others are filtered out. Whern null or empty array uses defaults application/json and text/html</param>
        /// <param name="logHandler"></param>
        /// <returns>The builder</returns>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder, string [] contentTypes = null, Func<ILogger<RequestProfilerModel>, RequestProfilerModel, Task> logHandler = null) =>
            builder.UseMiddleware<RequestResponseLoggingMiddleware>(contentTypes?.Length > 0 ? contentTypes : new[] { "application/json", "text/html" }, logHandler ?? DefaultLoggingHandler);
    }
}
