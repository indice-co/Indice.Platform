
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Register an Index html route via minimal apis for kickstarting the workflow designer ui.
/// </summary>
public static class WorkflowDesignerIndex
{
    /// <summary>
    /// Register an Index html route via minimal apis for kickstarting the workflow designer ui.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pathPrefix">The path to host the designer index page. Defaults to <strong>/workflow</strong></param>
    public static IEndpointRouteBuilder MapCasesWorkflowDesignerPage(this IEndpointRouteBuilder routes, PathString? pathPrefix = null) {
        routes.MapGet(pathPrefix ?? "/workflow", async context => {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(@"<html><body><h1>Hello world</h1></body></html>");
        });

        return routes;
    }
}
