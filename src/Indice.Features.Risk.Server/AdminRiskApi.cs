using Indice.Features.Risk.Core.Data.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Risk.Server;

/// <summary>
/// Contains operations that expose functionality for the risk administration
/// </summary>
public static class AdminRiskApi
{
    /// <summary>
    /// Registers the endpoints for the administration functionality
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapAdminRisk(this IEndpointRouteBuilder builder) {
        var options = builder.ServiceProvider.GetService<IOptions<RiskApiOptions>>()?.Value ?? new RiskApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}")
            .WithGroupName("risk")
            .WithTags("AdminRisk")
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

        // GET: api/risk-events
        group
            .MapGet("risk-events", AdminRiskApiHandlers.GetRiskEvents)
            .WithName(nameof(AdminRiskApiHandlers.GetRiskEvents))
            .WithSummary("Fetch risk events.")
            .Produces(StatusCodes.Status200OK, typeof(ResultSet<RiskEvent>));

        // GET: api/risk-results
        group
            .MapGet("risk-results", AdminRiskApiHandlers.GetRiskResults)
            .WithName(nameof(AdminRiskApiHandlers.GetRiskResults))
            .WithSummary("Fetch risk results.")
            .Produces(StatusCodes.Status200OK, typeof(ResultSet<DbAggregateRuleExecutionResult>));

        // GET: api/risk-rules
        group
            .AllowAnonymous()
            .MapGet("risk-rules", AdminRiskApiHandlers.GetRiskRules)
            .WithName(nameof(AdminRiskApiHandlers.GetRiskRules))
            .WithSummary("Fetch registered risk rules.")
            .Produces(StatusCodes.Status200OK, typeof(IEnumerable<string>));

        // GET: api/risk-rule/{ruleName}
        group
            .AllowAnonymous()
            .MapGet("risk-rule/{ruleName}", AdminRiskApiHandlers.GetRiskRuleOptions)
            .WithName(nameof(AdminRiskApiHandlers.GetRiskRuleOptions))
            .WithSummary("Fetch the configuration options given a rule name.")
            .Produces(StatusCodes.Status200OK, typeof(Dictionary<string, string>))
            .Produces(StatusCodes.Status404NotFound);

        // POST: api/risk-rule/{ruleName}
        group
            .AllowAnonymous()
            .MapPost("risk-rule/{ruleName}", AdminRiskApiHandlers.UpdateRiskRuleOptions)
            .WithName(nameof(AdminRiskApiHandlers.UpdateRiskRuleOptions))
            .WithSummary("Update the configuration options given a rule name.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return builder;
    }
}
