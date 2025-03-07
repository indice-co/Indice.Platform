using Indice.Features.Cases.Workflows;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CasesWorkflowOptions>>().Value;
        var uiendpoint = routes.MapGet(pathPrefix ?? "/workflow", CreateWorkflowDesignerPage(options.RegisterAuthentication))
                               .ExcludeFromDescription();
        if (options.RegisterAuthentication) {
            uiendpoint.RequireAuthorization(CasesWorkflowFeatureExtensions.WorkflowPolicy);
        }

        // These three catch the main routs of elsa and forward them to the host page which loads the dashboard app.
        routes.MapFallback("workflow-definitions/{**path}", CreateWorkflowDesignerPage(options.RegisterAuthentication));
        routes.MapFallback("workflow-instances/{**path}", CreateWorkflowDesignerPage(options.RegisterAuthentication));
        routes.MapFallback("workflow-registry/{**path}", CreateWorkflowDesignerPage(options.RegisterAuthentication));

        if (options.RegisterAuthentication) {
            routes.MapGet("/workflow-signout", async (HttpContext httpContext, IConfiguration configuration) => {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = configuration.GetHost() });
                await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = configuration.GetHost() });
            })
            .RequireAuthorization(CasesWorkflowFeatureExtensions.WorkflowPolicy)
            .ExcludeFromDescription();
        }

        return routes;
    }

    private static RequestDelegate CreateWorkflowDesignerPage(bool authenticated = false) {
        return async context => {
            context.Response.ContentType = "text/html";

            var serverUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var displayLogout = authenticated ? string.Empty : "display:none !important";
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
    <div class="elsa-bg-gray-800" style="float: right; padding-right: 20px; padding-top: 14px;{displayLogout}">
        <input class="elsa-px-8 elsa-py-1 elsa-border elsa-border-transparent elsa-rounded-md elsa-text-white elsa-bg-red-600 focus:elsa-outline-none hover:elsa-bg-red-700 active:bg-red-700"
               style="cursor:pointer;"
               type="button"
               value="Logout"
               onclick="window.location.href='/workflow-signout'" />
    </div>
    <elsa-studio-root server-url="{serverUrl}" monaco-lib-path="_content/Elsa.Designer.Components.Web/monaco-editor/min">
        <elsa-studio-dashboard></elsa-studio-dashboard>
    </elsa-studio-root>
</body>
</html>
""");
        };
    }
}
