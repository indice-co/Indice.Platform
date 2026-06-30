using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// my Case Types API
/// </summary>
internal static class MyCaseTypesApi
{
    /// <summary>Case types from the customer's perspective.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapMyCaseTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/my/case-types");

        group.WithTags("MyCases");
        group.WithGroupName("my");

        var allowedScopes = new[] { options.RequiredScope }.FilterOutNulls().ToArray();

        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Subject)
        ).WithHandledException<BusinessException>();

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(string.Empty, MyCaseTypesHandlers.GetCaseTypes)
            .WithName(nameof(MyCaseTypesHandlers.GetCaseTypes))
            .WithSummary("Gets case types.");

        group.MapGet("{caseTypeCode}", MyCaseTypesHandlers.GetCaseType)
            .WithName(nameof(MyCaseTypesHandlers.GetCaseType))
            .WithSummary("Gets a case type by its code.");

        return routes;
    }
}
