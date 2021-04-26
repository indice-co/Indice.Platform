using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Indice.AspNetCore.Filters
{
    /// <summary>
    /// Sets the accepted file extensions for the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AllowedFileExtensionsAttribute : Attribute, IActionFilter
    {
        private List<string> AllowedExtensions { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AllowedFileExtensionsAttribute"/>.
        /// </summary>
        /// <param name="fileExtensions">Allowed file extensions as a comma or space separated string.</param>
        public AllowedFileExtensionsAttribute(params string[] fileExtensions) {
            AllowedExtensions = fileExtensions.Select(x => x.Trim()).ToList();
        }

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context) {
            IEnumerable<IFormFile> files = context.HttpContext.Request.Form.Files;
            foreach (var file in files) {
                if (!AllowedExtensions.Any(x => Path.GetExtension(file.FileName.ToLowerInvariant()).EndsWith(x))) {
                    context.ModelState.AddModelError($"{file.FileName}", $"File with extension {Path.GetExtension(file.FileName)} is not permitted. Allowed file extensions are {string.Join(", ", AllowedExtensions)}");
                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
                }
            }
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
