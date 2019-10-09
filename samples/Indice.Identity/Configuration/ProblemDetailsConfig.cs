using System;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for configuring the problem-details feature.
    /// </summary>
    public static class ProblemDetailsConfig
    {
        /// <summary>
        /// Adds configuration for Standardizing error messages in API controllers.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        public static IServiceCollection AddProblemDetailsConfig(this IServiceCollection services, IWebHostEnvironment hostingEnvironment) {
            services.AddProblemDetails(options => {
                // This is the default behavior. Only include exception details in a development environment.
                options.IncludeExceptionDetails = _ => hostingEnvironment.IsDevelopment();
                // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
                // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
                options.Map<Exception>(exception => new ExceptionProblemDetails(exception, StatusCodes.Status500InternalServerError));
            });
            return services;
        }
    }
}
