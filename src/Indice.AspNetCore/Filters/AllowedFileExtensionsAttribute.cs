#nullable enable
using Indice.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Filters;

/// <summary>Sets the accepted file extensions for the request body.</summary>
/// <remarks>
/// .xlsx, .docx, .jpg
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AllowedFileExtensionsAttribute : Attribute, IActionFilter
{
    private List<string> _allowedExtensions { get; }

    /// <summary>Creates a new instance of <see cref="AllowedFileExtensionsAttribute"/>.</summary>
    /// <param name="fileExtensions">Allowed file extensions as a comma or space separated string.</param>
    /// <remarks>
    /// .xlsx, .docx, .jpg
    /// </remarks>
    public AllowedFileExtensionsAttribute(params string[] fileExtensions) {
        _allowedExtensions = fileExtensions.Select(x => '.' + x.Trim().TrimStart('.')).ToList();
    }

    /// <summary>Creates a new instance of <see cref="AllowedFileExtensionsAttribute"/>.</summary>
    public AllowedFileExtensionsAttribute() { 
        _allowedExtensions = new List<string>();
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context) {
        if (!_allowedExtensions.Any()) {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<LimitUploadOptions>>().Value;
            var extensions = options.DefaultAllowedFileExtensions.Select(x => '.' + x.Trim().TrimStart('.')).ToList();
            _allowedExtensions.AddRange(extensions);
        }

        IEnumerable<IFormFile> files = context.HttpContext.Request.Form.Files;
        foreach (var file in files) {
            if (!_allowedExtensions.Any(x => file.FileName.ToLowerInvariant().EndsWith(x))) {
                context.ModelState.AddModelError($"{file.FileName}", $"File with extension {Path.GetExtension(file.FileName)} is not permitted. Allowed file extensions are {string.Join(", ", _allowedExtensions)}");
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            }
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context) { }
}
#nullable disable
