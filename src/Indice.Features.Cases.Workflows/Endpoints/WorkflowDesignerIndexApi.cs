using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Register an Index html route via minimal apis for kickstarting the workflow designer ui.
/// </summary>
public static class WorkflowDesignerIndexApi
{
    /// <summary>
    /// Register an Index html route via minimal apis for kickstarting the workflow designer ui.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pathPrefix">The path to host the designer index page. Defaults to <strong>/workflow</strong></param>
    public static IEndpointRouteBuilder MapCasesWorkflowDesignerPage(this IEndpointRouteBuilder routes, PathString? pathPrefix = null) {
        routes.MapGet(pathPrefix ?? "/workflow", CreateWorkflowDesignerPage());


        // These three catch the main routs of elsa and forward them to the host page which loads the dashboard app.
        //app.MapFallbackToPage("workflow-definitions/{*path}", "/_Host");
        //app.MapFallbackToPage("workflow-instances/{*path}", "/_Host");
        //app.MapFallbackToPage("workflow-registry/{*path}", "/_Host");
        routes.MapFallback(CreateWorkflowDesignerPage());
        return routes;
    }

    private static RequestDelegate CreateWorkflowDesignerPage() {
        return async context => {
            context.Response.ContentType = "text/html";

            var serverUrl = $"{context.Request.Scheme}://{context.Request.Host}";

            await context.Response.WriteAsync($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Elsa Workflows</title>
    <link rel="icon" type="image/png" sizes="32x32" href="/_content/Elsa.Designer.Components.Web/elsa-workflows-studio/assets/images/favicon-32x32.png">
    <link rel="icon" type="image/png" sizes="16x16" href="/_content/Elsa.Designer.Components.Web/elsa-workflows-studio/assets/images/favicon-16x16.png">
    <link rel="stylesheet" href="/_content/Elsa.Designer.Components.Web/elsa-workflows-studio/assets/fonts/inter/inter.css">
    <link rel="stylesheet" href="/_content/Elsa.Designer.Components.Web/elsa-workflows-studio/elsa-workflows-studio.css">
    <script src="/_content/Elsa.Designer.Components.Web/monaco-editor/min/vs/loader.js"></script>
    <script type="module" src="/_content/Elsa.Designer.Components.Web/elsa-workflows-studio/elsa-workflows-studio.esm.js"></script>
</head>
<body>
    <elsa-studio-root server-url="{serverUrl}" monaco-lib-path="_content/Elsa.Designer.Components.Web/monaco-editor/min">
        <elsa-studio-dashboard></elsa-studio-dashboard>
    </elsa-studio-root>
</body>
</html>
""");
        };
    }
}
