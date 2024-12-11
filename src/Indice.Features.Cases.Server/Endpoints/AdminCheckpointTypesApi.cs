using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Manage check point types.</summary>
internal static class AdminCheckpointTypesApi
{
    /// <summary>Maps admin check point types endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCheckpointTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/checkpoint-types");

        group.WithTags("AdminCheckpointTypes");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] {
            options.RequiredScope
        }.Where(x => x != null).ToArray();

        group.RequireAuthorization(policy => policy
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(Authorization.CasesAccessLevel.Manager)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, AdminCheckpointTypesHandler.GetDistinctCheckpointTypes)
            .WithName(nameof(AdminCheckpointTypesHandler.GetDistinctCheckpointTypes))
            .WithSummary("Get the distinct checkpoint types grouped by code.");

        return group;
    }
}
