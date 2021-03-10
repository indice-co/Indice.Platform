using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Indice.AspNetCore.Filters
{
    /// <summary>
    /// Sets the maximum allowed file size for the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AllowedFileSizeAttribute : Attribute, IActionFilter
    {
        private readonly long _sizeLimit;

        /// <summary>
        /// Creates a new instance of <see cref="AllowedFileSizeAttribute"/>.
        /// </summary>
        /// <param name="sizeLimit">The maximum allowed file size in bytes.</param>
        public AllowedFileSizeAttribute(long sizeLimit) {
            _sizeLimit = sizeLimit;
        }

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context) {
            IEnumerable<IFormFile> files = context.HttpContext.Request.Form.Files;
            foreach (var file in files) {
                if (file.Length > _sizeLimit) {
                    context.ModelState.AddModelError($"{file.FileName}", $"File size cannot exceed {_sizeLimit} bytes.");
                    context.Result = new BadRequestObjectResult(context.ModelState);
                }
            }
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
