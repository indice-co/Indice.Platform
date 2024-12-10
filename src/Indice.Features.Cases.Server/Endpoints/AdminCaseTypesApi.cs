using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Case types from the administrative perspective.</summary>
public static class AdminCaseTypesApi
{
    /// <summary>Maps admin case types endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCaseTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/case-types");
        group.WithTags("AdminCaseTypes");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet("", AdminCaseTypesHandler.GetAdminCaseTypes)
             .WithName(nameof(AdminCaseTypesHandler.GetAdminCaseTypes))
             .WithSummary("Get case types.");
        group.MapGet("{caseTypeId}", AdminCaseTypesHandler.GetCaseTypeById)
            //.RequireAuthorization(CasesCo.Policies.BeCasesAdministrator)
            .WithSummary("Get a specific Case Type by Id.");
        group.MapPost("", AdminCaseTypesHandler.CreateCaseType)
            //.RequireAuthorization(CasesApiConstants.Policies.BeCasesAdministrator)
            .WithSummary("Create new case type.");
        group.MapPut("{caseTypeId}", AdminCaseTypesHandler.UpdateCaseType)
            //.RequireAuthorization(CasesApiConstants.Policies.BeCasesAdministrator)
            .WithSummary("Update a specific Case Type.");
        group.MapDelete("{caseTypeId}", AdminCaseTypesHandler.DeleteCaseType)
            //.RequireAuthorization(CasesApiConstants.Policies.BeCasesAdministrator)
            .WithSummary("Delete a specific Case Type.");
        return group;
    }
}
