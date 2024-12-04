using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Manage cases reports and everything related to cases for back-office users.</summary>
public static class AdminReportsApi
{
    /// <summary>Maps admin reports endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminReports(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
        var group = routes.MapGroup($"{options.ApiPrefix}/manage/reports");
        group.WithTags("AdminReports");
        group.WithGroupName(options.GroupName);
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
        group.MapGet("", AdminReportsHandler.GetCaseReport)
            .WithName(nameof(AdminReportsHandler.GetCaseReport))
                .WithSummary("Get case report");
        return group;
    }
}
