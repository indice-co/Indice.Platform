using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks;

/// <summary>Bookmark model for <see cref="AwaitEditActivity"/>.</summary>
public class AwaitEditBookmark(string caseId, string? role = null) : IBookmark
{
    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; } = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    [ExcludeFromHash]
    public string Role { get; set; } = role ?? string.Empty;
}

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
        return [
            Result(new AwaitEditBookmark(context.ActivityExecutionContext.CorrelationId, allowedRole))
        ];
    }
}