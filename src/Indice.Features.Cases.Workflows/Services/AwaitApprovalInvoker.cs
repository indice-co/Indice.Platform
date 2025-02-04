using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Services.Abstractions;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitApprovalInvoker(
    IWorkflowLaunchpad workflowLaunchpad,
    IWorkflowInstanceStore workflowInstanceStore)
    : BaseActivityInvoker(workflowLaunchpad, workflowInstanceStore), IAwaitApprovalInvoker
{
    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, InvokeApprovalRequest approvalRequest, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, approvalRequest, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, InvokeApprovalRequest approvalRequest, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, approvalRequest, cancellationToken);
    
    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) => [
        new WorkflowsQuery(
            ActivityType: nameof(AwaitApprovalActivity),
            Bookmark: new AwaitApprovalBookmark(caseId.ToString()),
            CorrelationId: caseId.ToString(),
            WorkflowInstanceId: (await GetWorkflowInstanceByCaseId(caseId, cancellationToken)).Id
        )
    ];
}