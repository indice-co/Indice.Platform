using Indice.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Filters;

/// <summary>Sets the maximum allowed file size for the request body.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AllowedFileSizeAttribute : Attribute, IActionFilter
{
    private readonly long? _sizeLimit;

    /// <summary>Creates a new instance of <see cref="AllowedFileSizeAttribute"/>.</summary>
    public AllowedFileSizeAttribute(long sizeLimit) {
        _sizeLimit = sizeLimit;
    }

    /// <summary>Creates a new instance of <see cref="AllowedFileSizeAttribute"/>.</summary>
    public AllowedFileSizeAttribute() {
        _sizeLimit = null;
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context) {
        var options = context.HttpContext.RequestServices.GetService<IOptions<AllowedFileSizeAttributeOptions>>()?.Value 
            ?? new AllowedFileSizeAttributeOptions();

        var allowedFileSize = _sizeLimit
            ?? options.AllowedFileSizeBytes;

        IEnumerable<IFormFile> files = context.HttpContext.Request.Form.Files;
        foreach (var file in files) {
            if (file.Length > allowedFileSize) {
                context.ModelState.AddModelError($"{file.FileName}", $"File size cannot exceed {allowedFileSize} bytes.");
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context) { }
}
