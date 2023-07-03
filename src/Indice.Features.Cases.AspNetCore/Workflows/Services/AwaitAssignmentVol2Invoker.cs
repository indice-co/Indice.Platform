using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitAssignmentVol2Invoker : BaseActivityInvoker, IAwaitAssignmentVol2Invoker
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AwaitAssignmentVol2Invoker(
        IWorkflowLaunchpad workflowLaunchpad,
        IHttpContextAccessor httpContextAccessor,
        IWorkflowInstanceStore workflowInstanceStore)
        : base(workflowLaunchpad, workflowInstanceStore) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, AwaitAssignmentInvokerInput input, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, input, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, AwaitAssignmentInvokerInput input, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, input, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) {
        var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);

        var userRoles = _httpContextAccessor.HttpContext.User
            .FindAll(x => x.Type == BasicClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles.Add(string.Empty);

        return userRoles.Select(role => new WorkflowsQuery(
            nameof(AwaitAssignmentVol2Activity),
            new AwaitAssignmentVol2Bookmark(caseId.ToString(), role),
            caseId.ToString(),
            instance.Id));
    }
}
