using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>CaseApproval service for handling case approvals entities.</summary>
public interface ICaseApprovalService
{
    /// <summary>Add an approval to a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="commentId">The Id of the comment (if any).</param>
    /// <param name="user">The actor.</param>
    /// <param name="action">The action of the actor.</param>
    /// <param name="reason">The reason of the rejection.</param>
    Task AddApproval(Guid caseId, Guid? commentId, ClaimsPrincipal user, Approval action, string? reason);

    /// <summary>Get the last <see cref="CaseApproval.Committed"/> approval (or null, if it does not exist) for a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>The last existing approval or null</returns>
    Task<CaseApproval?> GetLastApproval(Guid caseId);

    /// <summary>Rollback the last approval for a case. </summary>
    /// <param name="caseId">The Id of the case.</param>
    ValueTask RollbackApproval(Guid caseId);

    /// <summary>Get a list of rejected reasons as they have defined to the Workflow.</summary>
    /// <param name="caseId">The Id of the case</param>
    /// <returns>A list of <see cref="RejectReason"/></returns>
    ValueTask<List<RejectReason>> GetRejectReasons(Guid caseId);
}