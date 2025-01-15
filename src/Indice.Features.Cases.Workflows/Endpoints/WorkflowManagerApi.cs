using Indice.Features.Cases.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

public static class WorkflowManagerApi
{
    public static IEndpointRouteBuilder MapWorkflowManager(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CasesWorkflowOptions>>().Value;
        var group = routes.MapGroup("/workflow-manager");
        group.WithGroupName("workflow");
        group.WithTags("Workflow");
        
        // todo: add authentication
        // group.ProducesProblem(StatusCodes.Status401Unauthorized)
        //     .ProducesProblem(StatusCodes.Status403Forbidden)
        //     .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        group.MapPost("start-workflow", WorkflowManagerHandler.StartWorkflow)
            .WithName(nameof(WorkflowManagerHandler.StartWorkflow))
            .WithSummary("Start a workflow for a case id.");
        
        group.MapPost("invoke/approval", WorkflowManagerHandler.InvokeApproval)
            .WithName(nameof(WorkflowManagerHandler.InvokeApproval))
            .WithSummary("Submit approval for a case id.");
        
        group.MapPost("invoke/edit", WorkflowManagerHandler.InvokeEdit)
            .WithName(nameof(WorkflowManagerHandler.InvokeEdit))
            .WithSummary("Edit data of a case id.");
        
        group.MapGet("actions/{caseId}", WorkflowManagerHandler.GetActionsByCaseId)
            .WithName(nameof(WorkflowManagerHandler.GetActionsByCaseId))
            .WithSummary("Get all available actions for case id.");
        
        group.MapGet("available-actions/{caseId}", WorkflowManagerHandler.GetAvailableActions)
            .WithName(nameof(WorkflowManagerHandler.GetAvailableActions))
            .WithSummary("Obsolete Get all available actions for case id.");
        

        return group;
    }
}