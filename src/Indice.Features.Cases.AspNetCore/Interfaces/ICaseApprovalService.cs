using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// CaseApproval service for handling case approvals entities.
    /// </summary>
    internal interface ICaseApprovalService
    {
        /// <summary>
        /// Add an approval to a case.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="commentId">The Id of the comment (if any).</param>
        /// <param name="user">The actor.</param>
        /// <param name="action">The action of the actor.</param>
        /// <param name="reason">The reason of the rejection.</param>
        /// <returns></returns>
        Task AddApproval(Guid caseId, Guid? commentId, ClaimsPrincipal user, Approval action, string reason);
        
        /// <summary>
        /// Get the last <see cref="DbCaseApproval.Committed"/> approval (or null, if it does not exist) for a case.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        Task<DbCaseApproval> GetLastApproval(Guid caseId);

        /// <summary>
        /// Rollback the last approval for a case. 
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        ValueTask RollbackApproval(Guid caseId);

        /// <summary>
        /// Get a list of rejected reasons as they have defined to the Workflow.
        /// </summary>
        /// <param name="caseId">The Id of the case</param>
        /// <returns></returns>
        ValueTask<IEnumerable<RejectReason>> GetRejectReasons(Guid caseId);
    }
}