using System;
using Elsa.Attributes;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval
{
    /// <summary>
    /// Bookmark model for <see cref="AwaitApprovalActivity"/>.
    /// </summary>
    internal class AwaitApprovalBookmark : IBookmark
    {
        public AwaitApprovalBookmark(string caseId, string role, bool blockPreviousApprover = false) {
            CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
            Role = role;
            BlockPreviousApprover = blockPreviousApprover;
        }

        /// <summary>
        /// The Id of the case to create the bookmark.
        /// </summary>
        public string CaseId { get; set; }

        /// <summary>
        /// The user role that can trigger the bookmark. Can be null for all authenticated users
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Block previous approver from triggering the bookmark.
        /// </summary>
        [ExcludeFromHash]
        public bool BlockPreviousApprover { get; set; }
    }
}
