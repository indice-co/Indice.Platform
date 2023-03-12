using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitEditInvoker : BaseActivityInvoker, IAwaitEditInvoker
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AwaitEditInvoker(
        IWorkflowLaunchpad workflowLaunchpad,
        IWorkflowInstanceStore workflowInstanceStore,
        IHttpContextAccessor httpContextAccessor)
        : base(workflowLaunchpad, workflowInstanceStore) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, EditCaseRequest request, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, request.Data as object, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, EditCaseRequest request, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, request.Data as object, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) {
        var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);

        var userRoles = _httpContextAccessor.HttpContext?.User
            .FindAll(x => x.Type == BasicClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles?.Add(string.Empty);

        return userRoles?.Select(role => new WorkflowsQuery(
            nameof(AwaitEditActivity),
            new AwaitApprovalBookmark(caseId.ToString(), role),
            caseId.ToString(),
            instance.Id));
    }
}