using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Services.Abstractions;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitActionInvoker : BaseActivityInvoker, IAwaitActionInvoker
{
    /// <inheritdoc />
    public AwaitActionInvoker(IWorkflowLaunchpad workflowLaunchpad, IWorkflowInstanceStore workflowInstanceStore) : base(workflowLaunchpad, workflowInstanceStore) {
    }

    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, InvokeActionRequest actionId, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, actionId, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, InvokeActionRequest actionId, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, actionId, cancellationToken);

    protected override Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<WorkflowsQuery>()); // ignore this implementation

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries<TWorkflowInput>(
        Guid caseId,
        TWorkflowInput input,
        CancellationToken cancellationToken = default) => [
        new WorkflowsQuery (
            ActivityType: nameof(AwaitActionActivity),
            Bookmark: new AwaitActionBookmark(caseId.ToString(), (input as InvokeActionRequest)?.ActionId.ToString() ?? string.Empty),
            CorrelationId: caseId.ToString(),
            WorkflowInstanceId: (await GetWorkflowInstanceByCaseId(caseId, cancellationToken)).Id
        )
    ];
}