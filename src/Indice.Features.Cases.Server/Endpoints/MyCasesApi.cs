﻿using Indice.Features.Cases;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// my Cases API
/// </summary>
public static class MyCasesApi
{
    /// <summary>Case types from the customer's perspective.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapMyCases(this IEndpointRouteBuilder routes) {
        CaseServerEndpointOptions options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
        var group = routes.MapGroup($"{options.ApiPrefix}/my/case-types");
        group.WithTags("MyCases");
        group.WithGroupName(options.GroupName);
        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
             .RequireAuthenticatedUser()
             .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
             .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
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