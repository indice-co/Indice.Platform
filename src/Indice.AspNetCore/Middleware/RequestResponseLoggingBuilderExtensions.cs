using System;
using System.Collections.Generic;
using System.Text;
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
        /// Adds Request &amp; Response Logging feature to the <see cref="IApplicationBuilder"/> 
        /// request execution pipeline. Must be placed early on in the pipeline in order to catch the raw payload 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logHandler"></param>
        /// <returns>The builder</returns>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder, Action<ILogger<RequestProfilerModel>, RequestProfilerModel> logHandler = null) {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>(logHandler);
        }
    }
}
