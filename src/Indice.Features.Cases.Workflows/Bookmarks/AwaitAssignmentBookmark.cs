using Elsa.Attributes;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks;

/// <summary>Bookmark model for <see cref="AwaitAssignmentActivity"/>.</summary>
public class AwaitAssignmentBookmark : IBookmark
{
    /// <summary>Create a new <see cref="AwaitAssignmentBookmark"/> bookmark.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="role">The role to create the bookmark for.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AwaitAssignmentBookmark(string caseId, string? role = null) {
        Role = role;
        CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
    }

    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; }

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    [ExcludeFromHash]
    public string? Role { get; set; }
}

/// <summary>
/// The Bookmark provider to be invoked when Elsa indexes workflows when they get suspended.
/// <remarks>See <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities#bookmarks">Elsa Bookmarks documentation</a></remarks>
/// </summary>
internal class AwaitAssignmentBookmarkProvider : BookmarkProvider<AwaitAssignmentBookmark, AwaitAssignmentActivity>
{
    /// <summary>
    /// Creates a new <see cref="AwaitAssignmentBookmark"/> from the tuple (CaseId, AllowedRole) as taken from the context.
    /// When the <see cref="AwaitAssignmentInvoker"/> tries to find the correct bookmark to resume the corresponding activity,
    /// it will create a new <see cref=" AwaitAssignmentBookmark"/> with the same tuple (CaseId, UserRole).
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AwaitAssignmentActivity> context, CancellationToken cancellationToken) {
        var allowedRole = await context.ReadActivityPropertyAsync<AwaitAssignmentActivity, string>(x => x.AllowedRole!, cancellationToken) ?? string.Empty;
        return [
            Result(new AwaitAssignmentBookmark(context.ActivityExecutionContext.CorrelationId, allowedRole))
        ];
    }
}