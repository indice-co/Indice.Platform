using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitApprovalInvoker(
    IWorkflowLaunchpad workflowLaunchpad,
    IWorkflowInstanceStore workflowInstanceStore)
    : BaseActivityInvoker(workflowLaunchpad, workflowInstanceStore), IAwaitApprovalInvoker
{
    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, WorkflowSubmitApprovalRequest approvalRequest, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, approvalRequest, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, WorkflowSubmitApprovalRequest approvalRequest, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, approvalRequest, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries<TWorkflowInput>(
        Guid caseId,
        TWorkflowInput input,
        CancellationToken cancellationToken = default) {
        var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);
        var approvalRequest = input as WorkflowSubmitApprovalRequest;

        return approvalRequest?.Roles.Select(role => new WorkflowsQuery(
            nameof(AwaitApprovalActivity),
            new AwaitApprovalBookmark(caseId.ToString(), role),
            caseId.ToString(),
            instance.Id)) ?? [];
    }

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(
        Guid caseId, CancellationToken cancellationToken = default) {
        // return [];
        var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);
        // var userRoles = _httpContextAccessor.HttpContext!.User
        //     .FindAll(x => x.Type == BasicClaimTypes.Role)
        //     .Select(claim => claim.Value)
        //     .ToList();

        var userRoles = new List<string>();
        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles.Add(string.Empty);
        userRoles.Add("CasesAdministrator");


        return userRoles.Select(role => new WorkflowsQuery(
            nameof(AwaitApprovalActivity),
            new AwaitApprovalBookmark(caseId.ToString(), role),
            caseId.ToString(),
            instance.Id));
    }
}