using Indice.Features.Cases.Server.Authorization;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Endpoints for external workflow integration actions.</summary>
public static class CallbackApi
{

    /// <summary>
    /// Cases Access rules from the administrative perspective.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapCallbackWorkflow(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/workflow-external");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("Bearer")
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
                .RequireCasesAccess(CasesAccessLevel.Manage)
                .RequireCasesRecordAccess(CasesAccessLevel.Manage));

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("{caseId:guid}/{endpointName?}", CallbackHandler.SendExternalMessage)
            .WithName(nameof(CallbackHandler.SendExternalMessage))
            .WithSummary("Add a new Access rule for admin Users.");

        return group;
    }
}