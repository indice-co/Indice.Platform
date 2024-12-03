using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services;

internal class CaseApprovalService : ICaseApprovalService
{
    private readonly CasesDbContext _dbContext;
    //private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly CaseSharedResourceService _caseSharedResourceService;

    public CaseApprovalService(
        CasesDbContext dbContext,
        //IWorkflowInstanceStore workflowInstanceStore,
        CaseSharedResourceService caseSharedResourceService) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        //_workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
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

    public async Task<CaseApproval?> GetLastApproval(Guid caseId) {
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));

        return await _dbContext.CaseApprovals
                               .Where(p => p.CaseId == caseId && p.Committed)
                               .OrderByDescending(p => p.CreatedBy.When)
                               .Select(p => new CaseApproval {
                                   Id = p.Id,
                                   Action = p.Action,
                                   Committed = p.Committed,
                                   Reason = p.Reason,
                                   CreatedBy = p.CreatedBy,
                               }).FirstOrDefaultAsync();
    }

    public async ValueTask RollbackApproval(Guid caseId) {
        var approval = await GetLastApproval(caseId);
        if (approval == null) {
            throw new ArgumentNullException(nameof(approval), "Case is not valid.");
        }
        approval.Committed = false;
        await _dbContext.SaveChangesAsync();
    }

    
    /// <inheritdoc/>
    public ValueTask<List<RejectReason>> GetRejectReasons(Guid caseId) {
        //TODO: Workflow integration Or copy reasons in the cases side of things so we can drive this from the cases database instead of the workflow.
        throw new NotImplementedException();
    }
}