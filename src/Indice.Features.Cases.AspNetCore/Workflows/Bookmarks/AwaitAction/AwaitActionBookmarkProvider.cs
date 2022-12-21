using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitAction
{
    /// <summary>
    /// The Bookmark provider to be invoked when Elsa indexes workflows when they get suspended.
    /// <remarks>See <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities#bookmarks">Elsa Bookmarks documentation</a></remarks>
    /// </summary>
    internal class AwaitActionBookmarkProvider : BookmarkProvider<AwaitActionBookmark, AwaitActionActivity>
    {
        /// <summary>
        /// Creates a new <see cref="AwaitActionBookmark"/> from the tuple (CaseId, AllowedRole, ActionId) as taken from the context.
        /// When the <see cref="AwaitActionInvoker"/> tries to find the correct bookmark to resume the corresponding activity,
        /// it will create a new <see cref=" WorkflowsQuery"/> with the same tuple (CaseId, UserRole).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AwaitActionActivity> context, CancellationToken cancellationToken) {
            var role = await context.ReadActivityPropertyAsync<AwaitActionActivity, string>(x => x.AllowedRole!, cancellationToken) ?? string.Empty;
            var actionId = await context.ReadActivityPropertyAsync<AwaitActionActivity, string>(x => x.ActionId!, cancellationToken) ?? string.Empty;
            return new[] {
                // Create a bookmark for the activity's input role (or "" if left blank (that means bookmark will be triggered by an authenticated-only user))
                Result(new AwaitActionBookmark(context.ActivityExecutionContext.CorrelationId, role, actionId)),
                // Always create a bookmark for the administrator (also ignore blocking)
                Result(new AwaitActionBookmark(context.ActivityExecutionContext.CorrelationId, Security.BasicRoleNames.Administrator, actionId))
            };
        }
    }
}