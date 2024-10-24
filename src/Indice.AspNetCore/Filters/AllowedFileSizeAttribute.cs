using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Filters;

/// <summary>Sets the maximum allowed file size for the request body.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AllowedFileSizeAttribute : Attribute, IActionFilter
{
    private long _defaultSizeLimit;
    private readonly string _configurationKey;

    /// <summary>Creates a new instance of <see cref="AllowedFileSizeAttribute"/>.</summary>
    public AllowedFileSizeAttribute(long defaultSizeLimit, string configurationKey = null) {
        _configurationKey = configurationKey;
        _defaultSizeLimit = defaultSizeLimit;
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context) {
        var sizeLimit = _defaultSizeLimit;

        if (!string.IsNullOrWhiteSpace(_configurationKey)) {
            var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
            sizeLimit = configuration?.GetValue(_configurationKey, _defaultSizeLimit) ?? _defaultSizeLimit;
        }

        IEnumerable<IFormFile> files = context.HttpContext.Request.Form.Files;
        foreach (var file in files) {
            if (file.Length > sizeLimit) {
                context.ModelState.AddModelError($"{file.FileName}", $"File size cannot exceed {sizeLimit} bytes.");
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context) { }
}
