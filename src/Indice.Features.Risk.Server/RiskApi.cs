using Indice.Features.Risk.Core;
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
    /// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapRisk<TTransaction>(this IEndpointRouteBuilder builder) where TTransaction : TransactionBase {
        var options = builder.ServiceProvider.GetService<IOptions<RiskApiOptions>>()?.Value ?? new RiskApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}/risk")
                           .WithGroupName("risk")
                           .WithTags("Risk")
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden)
                           .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", new[] { options.ApiScope }.Where(x => x is not null).Cast<string>().ToArray());

        // POST: /api/risk/calculate
        group.MapPost("calculate", RiskApiHandlers.GetRisk<TTransaction>)
             .WithName(nameof(RiskApiHandlers.GetRisk))
             .WithSummary("Calculates the risk given the latest transaction presented in the system.");

        return builder;
    }

    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapRisk(this IEndpointRouteBuilder builder) => 
        builder.MapRisk<TransactionBase>();
}
