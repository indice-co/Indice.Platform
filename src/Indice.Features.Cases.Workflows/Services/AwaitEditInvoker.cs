using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Services.Abstractions;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitEditInvoker(IWorkflowLaunchpad workflowLaunchpad, IWorkflowInstanceStore workflowInstanceStore)
    : BaseActivityInvoker(workflowLaunchpad, workflowInstanceStore), IAwaitEditInvoker
{
    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, InvokeEditRequest request, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, request, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, InvokeEditRequest request, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, request, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) => [
        new WorkflowsQuery(
            ActivityType: nameof(AwaitEditActivity),
            Bookmark: new AwaitEditBookmark(caseId.ToString()),
            CorrelationId: caseId.ToString(),
            WorkflowInstanceId: (await GetWorkflowInstanceByCaseId(caseId, cancellationToken)).Id
        )
    ];
}