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
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
                .RequireCasesAccess(Authorization.CasesAccessLevel.Admin)
            ).WithHandledException<Exception>();
        
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Product Endpoints - Synchronous response
        group.MapGet("{caseId}", AdminWorkflowHandler.GetById)
            .WithName(nameof(AdminWorkflowHandler.GetById))
            .WithSummary("Gets an admin case.");
        
        group.MapGet("{caseId}/last-approval", AdminWorkflowHandler.GetLastApproval)
            .WithName(nameof(AdminWorkflowHandler.GetLastApproval))
            .WithSummary("Gets last approval of a case.");
        
        // Product Endpoints - Notifications but current or further activities down the line depend on completion of action
        group.MapPost("{caseId}/send-message", AdminWorkflowHandler.SendMessage)
            .WithName(nameof(AdminWorkflowHandler.SendMessage))
            .WithSummary("Sends a message for a case.");
        
        group.MapPost("{caseId}/assign", AdminWorkflowHandler.Assign)
            .WithName(nameof(AdminWorkflowHandler.Assign))
            .WithSummary("Assign case to a user.");
        
        group.MapPost("{caseId}/approve", AdminWorkflowHandler.AddApproval)
            .WithName(nameof(AdminWorkflowHandler.AddApproval))
            .WithSummary("Adds an approval to a case.");
        
        group.MapPost("{caseId}/approve-with-comment", AdminWorkflowHandler.AddApprovalWithComment)
            .WithName(nameof(AdminWorkflowHandler.AddApprovalWithComment))
            .WithSummary("Adds an approval with comment to a case translating the provided comment.");
        
        group.MapPost("{caseId}/remove-assignment", AdminWorkflowHandler.RemoveAssignment)
            .WithName(nameof(AdminWorkflowHandler.RemoveAssignment))
            .WithSummary("Removes the assigner of a case.");
        
        group.MapPost("{caseId}/block-previous-approver", AdminWorkflowHandler.BlockPreviousApprover)
            .WithName(nameof(AdminWorkflowHandler.BlockPreviousApprover))
            .WithSummary("Remove assignment and send a message for the UI.");
        
        group.MapPost("{caseId}/rollback-approval", AdminWorkflowHandler.RollbackApproval)
            .WithName(nameof(AdminWorkflowHandler.RollbackApproval))
            .WithSummary("Rollbacks the previous approval of a case.");
        
        // Integrator Endpoints
        group.MapPatch("{caseId}/patch-case-data", AdminWorkflowHandler.PatchData)
            .WithName(nameof(AdminWorkflowHandler.PatchData))
            .WithSummary("Patches the data for a case.");
        
        group.MapPatch("{caseId}/patch-case-metadata", AdminWorkflowHandler.PatchMetadata)
            .WithName(nameof(AdminWorkflowHandler.PatchMetadata))
            .WithSummary("Patches the metadata of a case.");
        
        return group;
    }
}