using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Services.Abstractions;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitAssignmentInvoker(
    IWorkflowLaunchpad workflowLaunchpad,
    IWorkflowInstanceStore workflowInstanceStore)
    : BaseActivityInvoker(workflowLaunchpad, workflowInstanceStore), IAwaitAssignmentInvoker
{
    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, InvokeAssignmentRequest input, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, input, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, InvokeAssignmentRequest input, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, input, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) => [
        new WorkflowsQuery(
            ActivityType: nameof(AwaitAssignmentActivity),
            Bookmark: new AwaitAssignmentBookmark(caseId.ToString()),
            CorrelationId: caseId.ToString(),
            WorkflowInstanceId: (await GetWorkflowInstanceByCaseId(caseId, cancellationToken)).Id
        )
    ];
}
