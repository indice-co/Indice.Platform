using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Dashboard figures for back-office users.</summary>
internal static class AdminDashboardApi
{
    /// <summary>Maps admin dashboard endpoints.</summary>
    public static IEndpointRouteBuilder MapAdminDashboard(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/dashboard");
        group.WithTags("AdminDashboard");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.FilterOutNulls().ToArray();

        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(CasesAccessLevel.Manage)
        ).WithHandledException<BusinessException>();

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("metrics", AdminDashboardHandlers.GetDashboardMetrics)
             .WithName(nameof(AdminDashboardHandlers.GetDashboardMetrics))
             .WithSummary("Get the dashboard metrics")
             .WithDescription("Case counts per status, my open cases, unassigned and stale open cases, and daily intake, computed over the cases visible to the current user.");

        return group;
    }
}
