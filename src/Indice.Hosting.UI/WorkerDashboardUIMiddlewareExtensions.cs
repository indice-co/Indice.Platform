using Indice.AspNetCore.EmbeddedUI;
using Indice.Hosting.UI;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Registers the workers dashboard SPA, served by the <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public static class WorkerDashboardUIMiddlewareExtensions
{
    /// <summary>Serves the workers dashboard single page application.</summary>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="optionsAction">Options for configuring the <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    public static IApplicationBuilder UseWorkerDashboardUI(this IApplicationBuilder builder, Action<WorkerDashboardUIOptions>? optionsAction = null) =>
        builder.UseSpaUI("browser", typeof(WorkerDashboardUIMiddlewareExtensions).Assembly, optionsAction);
}
