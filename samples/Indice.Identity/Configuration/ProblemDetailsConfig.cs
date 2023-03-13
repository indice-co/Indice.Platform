using Hellang.Middleware.ProblemDetails;
using Indice.Types;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains methods for configuring the problem-details feature.</summary>
public static class ProblemDetailsConfig
{
    /// <summary>Adds configuration for Standardizing error messages in API controllers.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
    public static IServiceCollection AddProblemDetailsConfig(this IServiceCollection services, IWebHostEnvironment hostingEnvironment) {
        services.AddProblemDetails(options => {
            // This is the default behavior; only include exception details in a development environment.
            options.IncludeExceptionDetails = (httpContext, exception) => hostingEnvironment.IsDevelopment();
            options.Map<BusinessException>(exception => {
                var response = new ValidationProblemDetails(exception.Errors) {
                    Title = exception.Message,
                    Status = StatusCodes.Status400BadRequest
                };
                response.Extensions["code"] = exception.Code;
                return response;
            });
            // This will map NotImplementedException to the 501 Not Implemented status code.
            options.Map<NotImplementedException>(exception => new StatusCodeProblemDetails(StatusCodes.Status501NotImplemented));
            // This will map HttpRequestException to the 503 Service Unavailable status code.
            options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);
            // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
            // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
            options.Map<Exception>(exception => new StatusCodeProblemDetails(StatusCodes.Status500InternalServerError));
        });
        return services;
    }
}
