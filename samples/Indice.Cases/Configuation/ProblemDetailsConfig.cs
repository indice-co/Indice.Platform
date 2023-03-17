using Elsa.Exceptions;
using Hellang.Middleware.ProblemDetails;
using Indice.Features.Cases.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Indice.Cases.Configuation;

public static class ProblemDetailsConfig
{
    public static IServiceCollection AddProblemDetailsConfig(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddProblemDetails(options => {
            // This is the default behavior; only include exception details in a development environment.
            options.IncludeExceptionDetails = (ctx, ex) => environment.IsDevelopment();
            #region Elsa
            options.Map<WorkflowException>(ex => new StatusCodeProblemDetails(StatusCodes.Status400BadRequest)); // <- More Elsa exceptions https://github.com/elsa-workflows/elsa-core/tree/5977989bcc8ff00808c8dc241d7b9c98583b9dc4/src/core/Elsa.Abstractions/Exceptions
            #endregion
            #region Cases
            // TODO add Business errors 400 errors here, otherwise these errors will be emmited as Status500InternalServerError
            options.Map<ValidationException>(ex => new ProblemDetails() { Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            options.Map<ResourceUnauthorizedException>(ex => new StatusCodeProblemDetails(StatusCodes.Status403Forbidden));
            options.Map<CaseNotFoundException>(ex => new StatusCodeProblemDetails(StatusCodes.Status404NotFound));
            #endregion
            // This will map NotImplementedException to the 501 Not Implemented status code.
            options.Map<NotImplementedException>(ex => new StatusCodeProblemDetails(StatusCodes.Status501NotImplemented));
            // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
            // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
            options.Map<Exception>(ex => new ProblemDetails() { Detail = ex.Message, Status = StatusCodes.Status500InternalServerError });
        });
        return services;
    }
}
