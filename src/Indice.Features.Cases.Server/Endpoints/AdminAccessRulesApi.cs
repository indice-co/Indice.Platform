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

/// <summary>Cases Access rules from the administrative perspective.</summary>
internal static class AdminAccessRulesApi
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
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
                .RequireCasesAccess(CasesAccessLevel.Manage))
            .WithHandledException<BusinessException>(); // equivalent to BeCasesManager

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("access-rules", AdminAccessRulesHandlers.GetAccessRules)
            .WithName(nameof(AdminAccessRulesHandlers.GetAccessRules))
            .WithSummary("Get Access rules.");

        group.MapPost("access-rules", AdminAccessRulesHandlers.CreateAccessRule)
            .WithName(nameof(AdminAccessRulesHandlers.CreateAccessRule))
            .WithSummary("Add a new Access rule for admin Users.")
            .RequireAuthorization(pb => pb.RequireCasesAccess(CasesAccessLevel.Administer))
            .WithParameterValidation<AddAccessRuleRequest>();

        group.MapPost("access-rules/batch", AdminAccessRulesHandlers.CreateAccessRulesBatch)
            .WithName(nameof(AdminAccessRulesHandlers.CreateAccessRulesBatch))
            .WithSummary("Add a new Access rule for admin Users.")
            .RequireAuthorization(pb => pb.RequireCasesAccess(CasesAccessLevel.Administer))
            .WithParameterValidation<List<AddAccessRuleRequest>>();

        group.MapPut("access-rules/{ruleId}/{accessLevel}", AdminAccessRulesHandlers.UpdateAccessRule)
            .WithName(nameof(AdminAccessRulesHandlers.UpdateAccessRule))
            .WithSummary("Update an existing Access rule.");

        group.MapDelete("access-rules/{ruleId}", AdminAccessRulesHandlers.DeleteAccessRule)
            .WithName(nameof(AdminAccessRulesHandlers.DeleteAccessRule))
            .WithSummary("Delete an existing Access rule.");

        group.MapGet("cases/{caseId}/access-rules", AdminAccessRulesHandlers.GetCaseAccessRules)
            .WithName(nameof(AdminAccessRulesHandlers.GetCaseAccessRules))
            .WithSummary("Get Access rules for the specified case.");

        group.MapPost("cases/{caseId}/access-rules", AdminAccessRulesHandlers.CreateCaseAccessRules)
            .WithName(nameof(AdminAccessRulesHandlers.CreateCaseAccessRules))
            .WithSummary("Add a new Access rule for a case.")
            .WithParameterValidation<AddCaseAccessRuleRequest>();

        group.MapPut("cases/{caseId}/access-rules/batch", AdminAccessRulesHandlers.UpdateCaseAccessRulesBatch)
            .WithName(nameof(AdminAccessRulesHandlers.UpdateCaseAccessRulesBatch))
            .WithSummary("Update a batch of Access rules for a case.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer))
            .WithParameterValidation<List<AddCaseAccessRuleRequest>>();
        
        group.MapPut("cases/{caseId}/access-rules/replace", AdminAccessRulesHandlers.ReplaceAccessRulesUser)
            .WithName(nameof(AdminAccessRulesHandlers.ReplaceAccessRulesUser))
            .WithSummary("Replace user to the specified case with another")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer))
            .WithParameterValidation<List<AddCaseAccessRuleRequest>>();

        return routes;
    }
}