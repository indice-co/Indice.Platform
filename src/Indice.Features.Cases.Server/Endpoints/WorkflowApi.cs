using Indice.Features.Cases.Server.Authorization;
using Indice.Security;
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
        group.MapGet("{caseId}", WorkflowHandler.GetById)
            .WithName(nameof(WorkflowHandler.GetById))
            .WithSummary("Gets an admin case.")
            .RequireAuthorization(pb => pb.RequireBeCasesMemberAccess(CasesAccessLevel.Manager));
        
        group.MapGet("{caseId}/last-approval", WorkflowHandler.GetLastApproval)
            .WithName(nameof(WorkflowHandler.GetLastApproval))
            .WithSummary("Gets last approval of a case.");
        
        // Product Endpoints - Notifications but current or further activities down the line depend on completion of action
        group.MapPost("{caseId}/send-message", WorkflowHandler.SendMessage)
            .WithName(nameof(WorkflowHandler.SendMessage))
            .WithSummary("Sends a message for a case.");
        
        group.MapPost("{caseId}/assign", WorkflowHandler.Assign)
            .WithName(nameof(WorkflowHandler.Assign))
            .WithSummary("Assign case to a user.");
        
        group.MapPost("{caseId}/approve", WorkflowHandler.AddApproval)
            .WithName(nameof(WorkflowHandler.AddApproval))
            .WithSummary("Adds an approval to a case.");
        
        group.MapPost("{caseId}/approve-with-comment", WorkflowHandler.AddApprovalWithComment)
            .WithName(nameof(WorkflowHandler.AddApprovalWithComment))
            .WithSummary("Adds an approval with comment to a case translating the provided comment.");
        
        group.MapPost("{caseId}/remove-assignment", WorkflowHandler.RemoveAssignment)
            .WithName(nameof(WorkflowHandler.RemoveAssignment))
            .WithSummary("Removes the assigner of a case.");
        
        group.MapPost("{caseId}/block-previous-approver", WorkflowHandler.BlockPreviousApprover)
            .WithName(nameof(WorkflowHandler.BlockPreviousApprover))
            .WithSummary("Remove assignment and send a message for the UI.");
        
        group.MapPost("{caseId}/rollback-approval", WorkflowHandler.RollbackApproval)
            .WithName(nameof(WorkflowHandler.RollbackApproval))
            .WithSummary("Rollbacks the previous approval of a case.");
        
        // Integrator Endpoints
        group.MapPatch("{caseId}/patch-case-data", WorkflowHandler.PatchData)
            .WithName(nameof(WorkflowHandler.PatchData))
            .WithSummary("Patches the data for a case.")
            .WithDescription(WorkflowHandler.PatchDataDescription);

        group.MapPatch("{caseId}/patch-case-metadata", WorkflowHandler.PatchMetadata)
            .WithName(nameof(WorkflowHandler.PatchMetadata))
            .WithSummary("Patches the metadata of a case.")
            .WithDescription(WorkflowHandler.PatchMetadataDescription)
            .RequireAuthorization(pb => pb.RequireBeCasesMemberAccess(CasesAccessLevel.Manager));
        
        return group;
    }
}