using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// my Cases API
/// </summary>
internal static class MyCasesApi
{
    /// <summary>Case types from the customer's perspective.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapMyCases(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/my/case-types");

        group.WithTags("MyCases");
        group.WithGroupName("my");

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();

        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        group.RequireAuthorization(policy => policy
             .RequireCasesAccess()
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesUser);

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("", MyCasesHandlers.GetCaseTypes)
             .WithName(nameof(MyCasesHandlers.GetCaseTypes))
             .WithSummary("Gets case types.");

        group.MapGet("{caseTypeCode}", MyCasesHandlers.GetCaseType)
             .WithName(nameof(MyCasesHandlers.GetCaseType))
             .WithSummary("Gets a case type by its code.");

        return routes;
    }
}
