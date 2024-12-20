using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit;

/// <summary>
/// The Bookmark provider to be invoked when Elsa indexes workflows when they get suspended.
/// <remarks>See <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities#bookmarks">Elsa Bookmarks documentation</a></remarks>
/// </summary>
internal class AwaitEditBookmarkProvider : BookmarkProvider<AwaitEditBookmark, AwaitEditActivity>
{
    /// <summary>
    /// Creates a new <see cref="AwaitEditBookmark"/> from the tuple (CaseId, AllowedRole) as taken from the context.
    /// When the <see cref="AwaitEditInvoker"/> tries to find the correct bookmark to resume the corresponding activity,
    /// it will create a new <see cref=" WorkflowsQuery"/> with the same tuple (CaseId, UserRole).
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AwaitEditActivity> context, CancellationToken cancellationToken) {
        var allowedRole = await context.ReadActivityPropertyAsync<AwaitEditActivity, string>(x => x.AllowedRole!, cancellationToken) ?? string.Empty;
        return new[] {
            // Create a bookmark for the activity's input role (or "" if left black (that means bookmark will be triggered by an authenticated-only user))
            Result(new AwaitEditBookmark(context.ActivityExecutionContext.CorrelationId, allowedRole)),
            // Always create a bookmark for the administrator
            Result(new AwaitEditBookmark(context.ActivityExecutionContext.CorrelationId, Indice.Security.BasicRoleNames.Administrator))
        };
    }
}