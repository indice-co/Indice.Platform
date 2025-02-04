using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class WorkflowApi
{
    public static IEndpointRouteBuilder MapWorkflow(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;
        // var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/cases-workflow");
        
        // group.WithTags("cases-workflow");
        // group.WithGroupName(options.GroupName);
        
        var group = routes.MapGroup("/manager-workflow");
        group.WithGroupName("manager-workflow");
        group.WithTags("manager-workflow");
        
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        // group.RequireAuthorization(policy => policy
        //         .RequireAuthenticatedUser()
        //         .AddAuthenticationSchemes("Bearer")
        //         .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        //     // .RequireCasesAccess(Authorization.CasesAccessLevel.Manager) // todo: remove policy probably 
        // );

        // todo: Internal Workflow specific endpoints but not all as the user is allowed to do whatever on the activity
        group.MapGet("get-case/{caseId}", WorkflowHandler.GetWorkflowCaseById)
            .WithName(nameof(WorkflowHandler.GetWorkflowCaseById))
            .WithSummary("Get workflow case.");
        
        group.MapPost("{caseId}/send-message", WorkflowHandler.SendMessage)
            .WithName(nameof(WorkflowHandler.SendMessage))
            .WithSummary("Sends a message for a case.");
        
        group.MapPost("{caseId}/assign", WorkflowHandler.Assign)
            .WithName(nameof(WorkflowHandler.Assign))
            .WithSummary("Assign case to a user.");
        
        group.MapPost("{caseId}/add-approval", WorkflowHandler.AddApproval)
            .WithName(nameof(WorkflowHandler.AddApproval))
            .WithSummary("Adds an approval to a case.");
        
        group.MapPost("{caseId}/add-approval-with-comment", WorkflowHandler.AddApprovalWithComment)
            .WithName(nameof(WorkflowHandler.AddApprovalWithComment))
            .WithSummary("Adds an approval with comment to a case.");
        
        group.MapGet("{caseId}/last-approval", WorkflowHandler.GetLastApproval)
            .WithName(nameof(WorkflowHandler.GetLastApproval))
            .WithSummary("Gets last approval of a case.");
        
        group.MapPost("{caseId}/remove-assignment", WorkflowHandler.RemoveAssignment)
            .WithName(nameof(WorkflowHandler.RemoveAssignment))
            .WithSummary("Removes the assigner of a case.");
        
        group.MapPost("{caseId}/rollback-approval", WorkflowHandler.RollbackApproval)
            .WithName(nameof(WorkflowHandler.RollbackApproval))
            .WithSummary("Rollbacks the previous approval of a case.");
        
        group.MapGet("contacts/{reference}/data/{caseTypeCode}", WorkflowHandler.GetContactReference)
            .WithName(nameof(WorkflowHandler.GetContactReference))
            .WithSummary("Fetch contact data by contact.reference number for a specific case type code.");
        
        
        
        return group;
    }
}