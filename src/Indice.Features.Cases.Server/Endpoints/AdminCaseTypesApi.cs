using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Case types from the administrative perspective.</summary>
internal static class AdminCaseTypesApi
{
    /// <summary>Maps admin case types endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCaseTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/case-types");
        group.WithTags("AdminCaseTypes");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] {
            options.RequiredScope
        }.Where(x => x != null).ToArray();

        group.RequireAuthorization(policy => policy
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(Authorization.CasesAccessLevel.Manager)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.MapGet(string.Empty, AdminCaseTypesHandler.GetAdminCaseTypes)
            .WithName(nameof(AdminCaseTypesHandler.GetAdminCaseTypes))
            .WithSummary("Get case types.");
        group.MapGet("{caseTypeId}", AdminCaseTypesHandler.GetCaseTypeById)
            .RequireAuthorization(policy => policy.RequireCasesAccess(Authorization.CasesAccessLevel.Manager))
            .WithSummary("Get a specific Case Type by Id.");
        group.MapPost(string.Empty, AdminCaseTypesHandler.CreateCaseType)
             .WithSummary("Create new case type.")
             .RequireAuthorization(policy => policy.RequireCasesAccess(Authorization.CasesAccessLevel.Admin)); // equivalent to BeCasesAdministrator
        group.MapPut("{caseTypeId}", AdminCaseTypesHandler.UpdateCaseType)
             .WithSummary("Update a specific Case Type.")
             .RequireAuthorization(policy => policy.RequireCasesAccess(Authorization.CasesAccessLevel.Admin)); // equivalent to BeCasesAdministrator
        group.MapDelete("{caseTypeId}", AdminCaseTypesHandler.DeleteCaseType)
             .WithSummary("Delete a specific Case Type.")
             .RequireAuthorization(policy => policy.RequireCasesAccess(Authorization.CasesAccessLevel.Admin)); // equivalent to BeCasesAdministrator
        return group;
    }
}
