using System.ComponentModel.DataAnnotations;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Case Data administrative actions.</summary>
public static class AdminCaseDataApi
{
    /// <summary>Case Data administrative actions.</summary>
    public static IEndpointRouteBuilder MapAdminCaseData(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/cases");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(pb => pb
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("Bearer")
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes))
            .RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPatch("{caseId:guid}/data", AdminCaseDataHandler.PatchAdminCaseData)
            .WithName(nameof(AdminCaseDataHandler.PatchAdminCaseData))
            .WithSummary("Patches the Case.Data object with an object passed in the body.");

        group.MapPatch("{caseId:guid}/data-json", AdminCaseDataHandler.JsonPatchAdminCaseData)
            .WithName(nameof(AdminCaseDataHandler.JsonPatchAdminCaseData))
            .WithSummary("Update the Case Data for the specific caseId according to https://datatracker.ietf.org/doc/html/rfc6902#appendix-A.");

        return routes;
    }
}
