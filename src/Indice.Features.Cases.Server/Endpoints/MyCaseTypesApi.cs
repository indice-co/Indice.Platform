using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>
/// my Case Types API
/// </summary>
public static class MyCaseTypesApi
{
    /// <summary>Case types from the customer's perspective.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapMyCaseTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/my/case-types");

        group.WithTags("MyCases");
        group.WithGroupName("my");

        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        group.RequireAuthorization(CasesApiConstants.Policies.BeCasesUser);

        group.WithOpenApi().AddOpenApiSecurityRequirement();
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(string.Empty, MyCaseTypesHandler.GetCaseTypes)
            .WithName(nameof(MyCaseTypesHandler.GetCaseTypes))
            .WithSummary("Gets case types.");

        group.MapGet("{caseTypeCode}", MyCaseTypesHandler.GetCaseType)
            .WithName(nameof(MyCaseTypesHandler.GetCaseType))
            .WithSummary("Gets a case type by its code.");

        return routes;
    }
}
