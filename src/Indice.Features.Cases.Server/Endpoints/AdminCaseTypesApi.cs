using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Case types from the administrative perspective.</summary>
internal static class AdminCaseTypesApi
{
    /// <summary>Maps admin case types endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCaseTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/case-types");
        group.WithTags("AdminCaseTypes");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();

        group.RequireAuthorization(pb => pb
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(CasesAccessLevel.Manage)
            ).WithHandledException<BusinessException>();
        
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.MapGet(string.Empty, AdminCaseTypesHandlers.GetCaseTypesList)
             .WithName(nameof(AdminCaseTypesHandlers.GetCaseTypesList))
             .WithSummary("Get case types.");
        group.MapGet("{caseTypeId}", AdminCaseTypesHandlers.GetCaseTypeById)
             .WithName(nameof(AdminCaseTypesHandlers.GetCaseTypeById))
             .WithSummary("Get a specific Case Type by Id.");
        group.MapPost(string.Empty, AdminCaseTypesHandlers.CreateCaseType)
             .WithName(nameof(AdminCaseTypesHandlers.CreateCaseType))
             .WithSummary("Create new case type.")
             .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)) // equivalent to BeCasesAdministrator
            .WithParameterValidation<CaseTypeRequest>();
        group.MapPut("{caseTypeId}", AdminCaseTypesHandlers.UpdateCaseType)
             .WithName(nameof(AdminCaseTypesHandlers.UpdateCaseType))
             .WithSummary("Update a specific Case Type.")
             .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)) // equivalent to BeCasesAdministrator
             .WithParameterValidation<CaseTypeRequest>();
        group.MapDelete("{caseTypeId}", AdminCaseTypesHandlers.DeleteCaseType)
             .WithName(nameof(AdminCaseTypesHandlers.DeleteCaseType))
             .WithSummary("Delete a specific Case Type.")
             .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)); // equivalent to BeCasesAdministrator
        return group;
    }
}
