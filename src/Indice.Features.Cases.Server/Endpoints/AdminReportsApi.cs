﻿using Indice.Security;
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
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/reports");
        group.WithTags("AdminReports");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();

        group.RequireAuthorization(policy => policy
             .RequireAuthenticatedUser()
             .AddAuthenticationSchemes("Bearer")
             .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );//.RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("", AdminReportsHandler.GetCaseReport)
             .WithName(nameof(AdminReportsHandler.GetCaseReport))
             .WithSummary("Get case report");

        return group;
    }
}
