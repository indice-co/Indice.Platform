using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Identity.Features
{
    internal sealed class ProblemDetailsExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context) {
            var webHostEnvironment = context.HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment));
            var problemDetails = new ProblemDetails {
                Status = StatusCodes.Status500InternalServerError
            };
            if (((IWebHostEnvironment)webHostEnvironment).IsDevelopment()) {
                problemDetails.Title = context.Exception.Message;
                problemDetails.Detail = context.Exception.InnerException?.Message;
            } else {
                problemDetails.Title = "An internal server error has occured.";
            }
            context.Result = new ObjectResult(problemDetails) {
                StatusCode = StatusCodes.Status500InternalServerError,
                ContentTypes = new MediaTypeCollection {
                    "application/problem+json"
                }
            };
        }
    }
}
