using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class AdminWorkflowApi
{
    public static IEndpointRouteBuilder MapWorkflow(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;
        
        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/workflow");
        group.WithGroupName("cases-workflow");
        group.WithTags("Cases workflow");
        
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("LocalBearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(Authorization.CasesAccessLevel.Admin)
        );

        group.MapGet("get-case/{caseId}", AdminWorkflowHandler.GetWorkflowCaseById)
            .WithName(nameof(AdminWorkflowHandler.GetWorkflowCaseById))
            .WithSummary("Get workflow case.");
        
        group.MapPost("{caseId}/send-message", AdminWorkflowHandler.SendMessage)
            .WithName(nameof(AdminWorkflowHandler.SendMessage))
            .WithSummary("Sends a message for a case.");
        
        group.MapPost("{caseId}/assign", AdminWorkflowHandler.Assign)
            .WithName(nameof(AdminWorkflowHandler.Assign))
            .WithSummary("Assign case to a user.");
        
        group.MapPost("{caseId}/add-approval", AdminWorkflowHandler.AddApproval)
            .WithName(nameof(AdminWorkflowHandler.AddApproval))
            .WithSummary("Adds an approval to a case.");
        
        group.MapPost("{caseId}/add-approval-with-comment", AdminWorkflowHandler.AddApprovalWithComment)
            .WithName(nameof(AdminWorkflowHandler.AddApprovalWithComment))
            .WithSummary("Adds an approval with comment to a case translating the provided comment.");
        
        group.MapGet("{caseId}/last-approval", AdminWorkflowHandler.GetLastApproval)
            .WithName(nameof(AdminWorkflowHandler.GetLastApproval))
            .WithSummary("Gets last approval of a case.");
        
        group.MapPost("{caseId}/remove-assignment", AdminWorkflowHandler.RemoveAssignment)
            .WithName(nameof(AdminWorkflowHandler.RemoveAssignment))
            .WithSummary("Removes the assigner of a case.");
        
        group.MapPost("{caseId}/block-previous-approver", AdminWorkflowHandler.BlockPreviousApprover)
            .WithName(nameof(AdminWorkflowHandler.BlockPreviousApprover))
            .WithSummary("Remove assignment and send a message.");
        
        group.MapPost("{caseId}/rollback-approval", AdminWorkflowHandler.RollbackApproval)
            .WithName(nameof(AdminWorkflowHandler.RollbackApproval))
            .WithSummary("Rollbacks the previous approval of a case.");
        
        group.MapGet("contacts/{reference}/data/{caseTypeCode}", AdminWorkflowHandler.GetContactReference)
            .WithName(nameof(AdminWorkflowHandler.GetContactReference))
            .WithSummary("Fetch contact data by contact.reference number for a specific case type code.");
        
        return group;
    }
}