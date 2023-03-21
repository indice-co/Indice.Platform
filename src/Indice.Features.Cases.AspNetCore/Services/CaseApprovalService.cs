using System.Globalization;
using System.Security.Claims;
using Elsa;
using Elsa.Persistence;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Features.Cases.Resources;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

internal class CaseApprovalService : ICaseApprovalService
{
    private readonly CasesDbContext _dbContext;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly CaseSharedResourceService _caseSharedResourceService;

    public CaseApprovalService(
        CasesDbContext dbContext,
        IWorkflowInstanceStore workflowInstanceStore,
        CaseSharedResourceService caseSharedResourceService) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
        _caseSharedResourceService = caseSharedResourceService ?? throw new ArgumentNullException(nameof(caseSharedResourceService));
    }

    public async Task AddApproval(Guid caseId, Guid? commentId, ClaimsPrincipal user, Approval action, string reason) {
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));
        if (user == null) throw new ArgumentNullException(nameof(user));

        await _dbContext.CaseApprovals.AddAsync(new DbCaseApproval {
            CreatedBy = AuditMeta.Create(user),
            Action = action,
            CaseId = caseId,
            CommentId = commentId,
            Committed = true, // It's always committed. If a workflow has the corresponding activity will be set to false (RemovePreviousApprovalActivity)
            Reason = reason
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task<DbCaseApproval> GetLastApproval(Guid caseId) {
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));

        return await _dbContext.CaseApprovals
            .AsQueryable()
            .OrderByDescending(p => p.CreatedBy.When)
            .FirstOrDefaultAsync(p => p.CaseId == caseId && p.Committed);
    }

    public async ValueTask RollbackApproval(Guid caseId) {
        var approval = await GetLastApproval(caseId);
        if (approval == null) {
            throw new ArgumentNullException(nameof(approval), "Case is not valid.");
        }
        approval.Committed = false;
        await _dbContext.SaveChangesAsync();
    }

    public async ValueTask<IEnumerable<RejectReason>> GetRejectReasons(Guid caseId) {
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));

        var instance = await _workflowInstanceStore.FindByCorrelationIdAsync(caseId.ToString());
        if (instance == null) {
            return Enumerable.Empty<RejectReason>();
        }

        var reasonsWorkflowVariable = instance.Variables.Get<IEnumerable<string>>(CasesApiConstants.WorkflowVariables.RejectReasons) ?? Enumerable.Empty<string>();
        var reasons = reasonsWorkflowVariable.Select(item => new RejectReason {
            Key = item,
            Value = _caseSharedResourceService.GetLocalizedHtmlString(item, CultureInfo.CurrentCulture.Name).Value
        });
        return reasons;
    }
}