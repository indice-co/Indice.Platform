using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
/// <summary>Manage check point types.</summary>
public static class AdminCheckpointTypesApi
{
    /// <summary>Maps admin check point types endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCheckpointTypes(this IEndpointRouteBuilder routes) {
        CaseServerEndpointOptions options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
        var group = routes.MapGroup($"{options.ApiPrefix}/manage/checkpoint-types");
        group.WithTags("AdminCheckpointTypes");
        group.WithGroupName("Cases");
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();

        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );//.RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet("", AdminCheckpointTypesHandler.GetDistinctCheckpointTypes)
             .WithName(nameof(AdminCheckpointTypesHandler.GetDistinctCheckpointTypes))
             .WithSummary("Get the distinct checkpoint types grouped by code.");
        return group;
    }
}
