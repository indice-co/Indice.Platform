using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indice.AspNetCore.Filters;

/// <summary>Sets the Content Security policy header for the current action.</summary>
public sealed class SecurityHeadersAttribute : ActionFilterAttribute
{
    /// <summary>Constructor defaults to allowing self origin, plus Google for fonts and scripts (Google cdn) and wildcard for images.</summary>
    public SecurityHeadersAttribute() { }

    /// <summary>Code that executes before an action is executed.</summary>
    /// <param name="context">A context for result filters, specifically <see cref="IResultFilter.OnResultExecuting(ResultExecutingContext)"/></param>
    public override void OnResultExecuting(ResultExecutingContext context) {
        var result = context.Result;
        if (result is ViewResult || result is PageResult) {
            context.HttpContext.ApplySecurityHeaders();
        }
    }
}
