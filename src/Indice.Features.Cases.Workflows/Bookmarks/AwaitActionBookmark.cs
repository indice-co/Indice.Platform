using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks;

/// <summary>Bookmark model for <see cref="AwaitActionActivity"/>.</summary>
public class AwaitActionBookmark : IBookmark
{
    /// <summary>AwaitActionBookmark Constructor</summary>
    public AwaitActionBookmark(string caseId, string actionId, string? role = null) {
        CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
        ActionId = actionId;
        Role = role;
    }

    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; }
    
    /// <summary>The Id of the action that represents this bookmark.</summary>
    public string ActionId { get; }

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    [ExcludeFromHash]
    public string? Role { get; set; }
}

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
        return [
            Result(new AwaitActionBookmark(context.ActivityExecutionContext.CorrelationId, actionId, role))
        ];
    }
}