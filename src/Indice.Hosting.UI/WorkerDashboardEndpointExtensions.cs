using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Maps the workers dashboard API.</summary>
public static class WorkerDashboardEndpointExtensions
{
    /// <summary>Maps the workers dashboard minimal API under <c>WorkerDashboardOptions.PathPrefix</c> (default <c>/api/workers</c>). Requires <c>AddWorkerDashboard()</c>.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application.</param>
    public static IEndpointRouteBuilder MapWorkerDashboard(this IEndpointRouteBuilder routes) {
        routes.MapWorkerDashboardApi();
        return routes;
    }
}
