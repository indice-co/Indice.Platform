using System;
using System.Net.Http;
using Hellang.Middleware.ProblemDetails;
using Indice.Features.Messages.Core.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProblemDetailsConfig
    {
        public static IServiceCollection AddProblemDetailsConfig(this IServiceCollection services, IWebHostEnvironment environment) {
            services.AddProblemDetails(options => {
                // This is the default behavior; only include exception details in a development environment.
                options.IncludeExceptionDetails = (httpContext, exception) => environment.IsDevelopment();
                options.Map<CampaignException>(exception => {
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
}
