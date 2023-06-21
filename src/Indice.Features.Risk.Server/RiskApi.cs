using Indice.Features.Risk.Core.Data.Models;
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
    /// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
    /// <typeparam name="TTransactionRequest">The type of transaction that the engine manages.</typeparam>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapRisk<TTransaction, TTransactionRequest>(this IEndpointRouteBuilder builder)
        where TTransaction : Transaction, new()
        where TTransactionRequest : CreateTransactionRequestBase<TTransaction> {
        var options = builder.ServiceProvider.GetService<IOptions<RiskApiOptions>>()?.Value ?? new RiskApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}/risk")
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

        // POST: /api/risk/calculate
        group.MapPost("calculate", RiskApiHandlers.GetTransactionRisk<TTransaction, TTransactionRequest>)
             .WithName(nameof(RiskApiHandlers.GetTransactionRisk))
             .WithSummary("Calculates the risk given a transaction presented in the system.");

        // POST: /api/risk/events
        group.MapPost("events", RiskApiHandlers.AddEvent)
             .WithName(nameof(RiskApiHandlers.AddEvent))
             .WithSummary("Accepts and stores an event.");
        //.WithParameterValidation<CreateTransactionEventCommand>();

        return builder;
    }

    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapRisk(this IEndpointRouteBuilder builder) =>
        builder.MapRisk<Transaction, CreateTransactionRequestBase<Transaction>>();
}
