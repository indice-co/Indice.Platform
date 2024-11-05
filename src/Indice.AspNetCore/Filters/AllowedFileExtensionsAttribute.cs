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
    private HashSet<string> _allowedExtensions;

    /// <summary>Creates a new instance of <see cref="AllowedFileExtensionsAttribute"/>.</summary>
    /// <param name="fileExtensions">Allowed file extensions as a comma or space separated string.</param>
    /// <remarks>
    /// .xlsx, .docx, .jpg
    /// </remarks>
    public AllowedFileExtensionsAttribute(params string[] fileExtensions) {
        _allowedExtensions = fileExtensions.Select(x => '.' + x.Trim().TrimStart('.')).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Creates a new instance of <see cref="AllowedFileExtensionsAttribute"/>.</summary>
    public AllowedFileExtensionsAttribute() {
        _allowedExtensions = [];
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context) {
        if (_allowedExtensions.Count == 0) {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<LimitUploadOptions>>().Value;
            _allowedExtensions = options.DefaultAllowedFileExtensions.Select(x => '.' + x.Trim().TrimStart('.')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        IEnumerable<IFormFile> files = context.HttpContext.Request.Form.Files;
        foreach (var file in files) {
            var extension = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(extension)) {
                context.ModelState.AddModelError($"{file.FileName}", $"File with extension {Path.GetExtension(file.FileName)} is not permitted. Allowed file extensions are {string.Join(", ", _allowedExtensions)}");
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            }
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context) { }
}
#nullable disable
