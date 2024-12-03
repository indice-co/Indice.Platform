using System.Globalization;
using System.Security.Claims;
using Elsa;
using Elsa.Persistence;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Services;

/// <summary>
/// Decorates <see cref="ICaseApprovalService"/> with the ability to retrieve Reject reasons via Workflow directly.
/// </summary>
public class WorkflowCaseApprovalService : ICaseApprovalService
{
    private readonly ICaseApprovalService _inner;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly CaseSharedResourceService _caseSharedResourceService;

    /// <inheritdoc/>
    public WorkflowCaseApprovalService(ICaseApprovalService inner,
        IWorkflowInstanceStore workflowInstanceStore,
        CaseSharedResourceService caseSharedResourceService) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner)); 
        _workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
        _caseSharedResourceService = caseSharedResourceService ?? throw new ArgumentNullException(nameof(caseSharedResourceService));
    }

    /// <inheritdoc/>
    public Task AddApproval(Guid caseId, Guid? commentId, ClaimsPrincipal user, Approval action, string reason) =>
        _inner.AddApproval(caseId, commentId, user, action, reason);

    /// <inheritdoc/>
    public Task<CaseApproval?> GetLastApproval(Guid caseId) => _inner.GetLastApproval(caseId);

    /// <inheritdoc/>
    public ValueTask RollbackApproval(Guid caseId) => _inner.RollbackApproval(caseId);

    /// <inheritdoc/>
    public async ValueTask<List<RejectReason>> GetRejectReasons(Guid caseId) {
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));

        var instance = await _workflowInstanceStore.FindByCorrelationIdAsync(caseId.ToString());
        if (instance == null) {
            return [];
        }

        var reasonsWorkflowVariable = instance.Variables.Get<IEnumerable<string>>(CasesApiConstants.WorkflowVariables.RejectReasons) ?? Enumerable.Empty<string>();
        var reasons = reasonsWorkflowVariable.Select(item => new RejectReason {
            Key = item,
            Value = _caseSharedResourceService.GetLocalizedHtmlString(item, CultureInfo.CurrentCulture.Name).Value
        });
        return reasons.ToList();
    }
}
