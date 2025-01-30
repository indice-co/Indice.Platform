using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Services;

internal class AwaitAssignmentInvoker : BaseActivityInvoker, IAwaitAssignmentInvoker
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AwaitAssignmentInvoker(
        IWorkflowLaunchpad workflowLaunchpad,
        IHttpContextAccessor httpContextAccessor,
        IWorkflowInstanceStore workflowInstanceStore)
        : base(workflowLaunchpad, workflowInstanceStore) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, WorkflowAssignCaseRequest input, CancellationToken cancellationToken = default) =>
        base.DispatchWorkflowsAsync(caseId, input, cancellationToken);

    public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, WorkflowAssignCaseRequest input, CancellationToken cancellationToken = default) =>
        base.ExecuteWorkflowsAsync(caseId, input, cancellationToken);

    protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) {
        var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);

        // var userRoles = _httpContextAccessor.HttpContext!.User
        //     .FindAll(x => x.Type == BasicClaimTypes.Role)
        //     .Select(claim => claim.Value)
        //     .ToList();

        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        // userRoles.Add(string.Empty);

        return new List<WorkflowsQuery> {
            new(
                nameof(AwaitAssignmentActivity),
                new AwaitAssignmentBookmark(caseId.ToString()),
                caseId.ToString(),
                instance.Id)
        };
        // return userRoles.Select(role => new WorkflowsQuery(
        //     nameof(AwaitAssignmentActivity),
        //     new AwaitAssignmentBookmark(caseId.ToString(), role),
        //     caseId.ToString(),
        //     instance.Id));
    }
}
