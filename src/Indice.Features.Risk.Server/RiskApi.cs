using Indice.Features.Risk.Server;
using Indice.Features.Risk.Server.Models;
using Indice.Security;
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
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapRisk(this IEndpointRouteBuilder builder) {
        var options = builder.ServiceProvider.GetService<IOptions<RiskApiOptions>>()?.Value ?? new RiskApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}")
                           .WithGroupName("risk")
                           .WithTags("Risk")
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden)
                           .ProducesProblem(StatusCodes.Status500InternalServerError)
                           .RequireAuthorization(policy => policy
                              .AddAuthenticationSchemes(options.AuthenticationScheme)
                              .RequireAuthenticatedUser()
                              .RequireAssertion(context => context.User.HasScope(options.ApiScope) || context.User.IsSystemClient())
                           );
        var requiredScopes = options.ApiScope.Split(' ').Where(scope => !string.IsNullOrWhiteSpace(scope)).ToArray();
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", requiredScopes);

        // POST: /api/risk/events
        group.MapGet("risk/events", RiskApiHandlers.CreateRiskEvent)
             .WithName(nameof(RiskApiHandlers.CreateRiskEvent))
             .WithSummary("Records a risk event in the store.");

        // POST: /api/risk
        group.MapPost("risk", RiskApiHandlers.GetRisk)
             .WithName(nameof(RiskApiHandlers.GetRisk))
             .WithSummary("Calculates the risk given a transaction presented in the system.")
             .WithParameterValidation<RiskModel>();

        return builder;
    }
}
