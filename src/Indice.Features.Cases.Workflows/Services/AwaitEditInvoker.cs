using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitEditInvoker(IWorkflowLaunchpad workflowLaunchpad, IWorkflowInstanceStore workflowInstanceStore)
    : BaseActivityInvoker(workflowLaunchpad, workflowInstanceStore), IAwaitEditInvoker
{
    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, WorkflowEditCaseRequest request, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, request, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, WorkflowEditCaseRequest request, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, request, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) {
        var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);

        // var userRoles = _httpContextAccessor.HttpContext?.User
        //     .FindAll(x => x.Type == BasicClaimTypes.Role)
        //     .Select(claim => claim.Value)
        //     .ToList();
        //
        // // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        // userRoles?.Add(string.Empty);

        return new List<WorkflowsQuery> {
            new(
                nameof(AwaitEditActivity),
                new AwaitEditBookmark(caseId.ToString(), string.Empty),
                caseId.ToString(),
                instance.Id)
        };
        // return userRoles?.Select(role => new WorkflowsQuery(
        //     nameof(AwaitEditActivity),
        //     new AwaitEditBookmark(caseId.ToString()),
        //     caseId.ToString(),
        //     instance.Id)) ?? [];
    }
}