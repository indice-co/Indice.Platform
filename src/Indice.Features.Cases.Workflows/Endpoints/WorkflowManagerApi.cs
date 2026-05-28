using Indice.Features.Cases.Workflows;
using Indice.Features.Cases.Workflows.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Invoking Workflow Activities for blocked instances.</summary>
public static class WorkflowManagerApi
{
    /// <summary>Invoking Workflow Activities for blocked instances.</summary>
    public static IEndpointRouteBuilder MapWorkflowManager(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup($"api/workflow-actions");
        group.WithGroupName("workflow");
        group.WithTags("Workflow");

        group.RequireAuthorization(CasesWorkflowFeatureExtensions.WorkflowPolicy);

        group.MapPost("start-workflow", WorkflowManagerHandler.StartWorkflow)
            .WithName(nameof(WorkflowManagerHandler.StartWorkflow))
            .WithSummary("Start a workflow for a case id.");
        
        group.MapPost("invoke/assign", WorkflowManagerHandler.InvokeAssignment)
            .WithName(nameof(WorkflowManagerHandler.InvokeAssignment))
            .WithSummary("Trigger an assignment activity.");
        
        group.MapPost("invoke/approval", WorkflowManagerHandler.InvokeApproval)
            .WithName(nameof(WorkflowManagerHandler.InvokeApproval))
            .WithSummary("Trigger an approval activity.");
        
        group.MapPost("invoke/edit", WorkflowManagerHandler.InvokeEdit)
            .WithName(nameof(WorkflowManagerHandler.InvokeEdit))
            .WithSummary("Trigger an edit activity.");
            
        group.MapPost("invoke/action", WorkflowManagerHandler.InvokeAction)
            .WithName(nameof(WorkflowManagerHandler.InvokeAction))
            .WithSummary("Trigger a custom action activity.");
        
        group.MapGet("actions/{caseId}", WorkflowManagerHandler.GetActionsByCaseId)
            .WithName(nameof(WorkflowManagerHandler.GetActionsByCaseId))
            .WithSummary("Get all available actions for case id.");
        
        group.MapGet("{caseId}/reject-reasons", WorkflowManagerHandler.GetRejectReasonsByCaseId)
            .WithName(nameof(WorkflowManagerHandler.GetRejectReasonsByCaseId))
            .WithSummary("Get the reject reasons for a case.");

        return group;
    }
}