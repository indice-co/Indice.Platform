using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class CaseApprovalService : ICaseApprovalService
    {
        private readonly CasesDbContext _dbContext;

        public CaseApprovalService(CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task AddApproval(Guid caseId, Guid? commentId, ClaimsPrincipal user, Approval action) {
            if (caseId == default) throw new ArgumentNullException(nameof(caseId));
            if (user == null) throw new ArgumentNullException(nameof(user));

            await _dbContext.CaseApprovals.AddAsync(new DbCaseApproval {
                CreatedBy = AuditMeta.Create(user),
                Action = action,
                CaseId = caseId,
                CommentId = commentId,
                Committed = true // It's always committed. If a workflow has the corresponding activity will be set to false (RollbackApprovalActivity)
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task<DbCaseApproval?> GetLastApproval(Guid caseId) {
            if (caseId == default) throw new ArgumentNullException(nameof(caseId));

            return await _dbContext.CaseApprovals
                .AsQueryable()
                .OrderByDescending(p => p.CreatedBy.When)
                .FirstOrDefaultAsync(p => p.CaseId == caseId && p.Committed);
        }

        public async Task RollbackApproval(Guid caseId) {
            var approval = await GetLastApproval(caseId);
            if (approval == null) {
                throw new ArgumentNullException(nameof(approval), "Case is not valid.");
            }
            approval.Committed = false;
            await _dbContext.SaveChangesAsync();
        }
    }
}