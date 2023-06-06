using Indice.Features.Risk.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Contains operations that expose functionality of the risk engine.</summary>
public static class RiskApi
{
    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The builder instance.</returns>
    public static IEndpointRouteBuilder MapRisk(this IEndpointRouteBuilder builder) {
        var options = builder.ServiceProvider.GetService<IOptions<RiskApiOptions>>()?.Value ?? new RiskApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}/risk").ExcludeFromDescription();
        return builder;
    }
}
