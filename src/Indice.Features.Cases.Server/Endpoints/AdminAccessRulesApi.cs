using System.ComponentModel.DataAnnotations;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Cases Access rules from the administrative perspective.</summary>
public static class AdminAccessRulesApi
{

    /// <summary>
    /// Cases Access rules from the administrative perspective.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapAdminAccessRules(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage");
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

        group.MapGet("access-rules", AdminAccessRulesHandler.GetAccessRules)
            .WithName(nameof(AdminAccessRulesHandler.GetAccessRules))
            .WithSummary("Get Access rules.");

        group.MapGet("cases/{caseId:Guid}/access-rules", AdminAccessRulesHandler.GetAccessRulesForCase)
            .WithName(nameof(AdminAccessRulesHandler.GetAccessRulesForCase))
            .WithSummary("Get Access rules for the specified case.");

        group.MapPost("access-rules/admin", AdminAccessRulesHandler.CreateAccessRuleAdmin)
            .WithName(nameof(AdminAccessRulesHandler.CreateAccessRuleAdmin))
            .WithSummary("Add a new Access rule for admin Users.")
            .RequireAuthorization(CasesApiConstants.Policies.BeCasesAdministrator);

        group.MapPost("access-rules/admin/batch", AdminAccessRulesHandler.CreateBatchAccessRulesAdmin)
            .WithName(nameof(AdminAccessRulesHandler.CreateBatchAccessRulesAdmin))
            .WithSummary("Add a new Access rule for admin Users.")
            .RequireAuthorization(CasesApiConstants.Policies.BeCasesAdministrator);

        group.MapPost("access-rules/case/{caseId:guid}", AdminAccessRulesHandler.CreateAccessRules)
            .WithName(nameof(AdminAccessRulesHandler.CreateAccessRules))
            .WithSummary("Add a new Access rule for a case.");

        group.MapPut("access-rules/case/{caseId:guid}/batch", AdminAccessRulesHandler.UpdateBatchAccessRules)
            .WithName(nameof(AdminAccessRulesHandler.UpdateBatchAccessRules))
            .WithSummary("Update a batch of Access rules for a case.")
            .RequireAuthorization(CasesApiConstants.Policies.BeCasesAdministrator);

        group.MapPut("access-rules/{ruleId:guid}/{accessLevel:int}", AdminAccessRulesHandler.UpdateAccessRule)
            .WithName(nameof(AdminAccessRulesHandler.UpdateAccessRule))
            .WithSummary("Update an existing Access rule.");

        group.MapDelete("access-rules/{ruleId:guid}", AdminAccessRulesHandler.DeleteAccessRule)
            .WithName(nameof(AdminAccessRulesHandler.DeleteAccessRule))
            .WithSummary("Delete an existing Access rule.");

        return routes;
    }
}
