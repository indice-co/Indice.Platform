using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval
{
    /// <summary>
    /// The Bookmark provider to be invoked when Elsa indexes workflows when they get suspended.
    /// <remarks>See <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities#bookmarks">Elsa Bookmarks documentation</a></remarks>
    /// </summary>
    internal class AwaitApprovalBookmarkProvider : BookmarkProvider<AwaitApprovalBookmark, AwaitApprovalActivity>
    {
        /// <summary>
        /// Creates a new <see cref="AwaitApprovalBookmark"/> from the tuple (CaseId, AllowedRole) as taken from the context.
        /// When the <see cref="AwaitApprovalInvoker"/> tries to find the correct bookmark to resume the corresponding activity,
        /// it will create a new <see cref=" WorkflowsQuery"/> with the same tuple (CaseId, UserRole).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AwaitApprovalActivity> context, CancellationToken cancellationToken) {
            var allowedRole = await context.ReadActivityPropertyAsync<AwaitApprovalActivity, string>(x => x.AllowedRole!, cancellationToken) ?? string.Empty;
            var blockPreviousApprover = await context.ReadActivityPropertyAsync<AwaitApprovalActivity, bool>(x => x.BlockPreviousApprover, cancellationToken);
            return new[] {
                // Create a bookmark for the activity's input role (or "" if left blank (that means bookmark will be triggered by an authenticated-only user))
                Result(new AwaitApprovalBookmark(context.ActivityExecutionContext.CorrelationId, allowedRole, blockPreviousApprover)),
                // Always create a bookmark for the administrator (also ignore blocking)
                Result(new AwaitApprovalBookmark(context.ActivityExecutionContext.CorrelationId, Security.BasicRoleNames.Administrator)) 
            };
        }
    }
}