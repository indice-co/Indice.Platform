using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitAction
{
    /// <summary>
    /// Bookmark model for <see cref="AwaitActionActivity"/>.
    /// </summary>
    internal class AwaitActionBookmark : IBookmark
    {
        public AwaitActionBookmark(string caseId, string role, string actionId) {
            CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
            Role = role;
            ActionId = actionId;
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
        /// The Id of the action that represents this bookmark.
        /// </summary>
        public string ActionId { get; }
    }
}
