using Elsa.Attributes;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Workflows.Bookmarks;

/// <summary>Bookmark model for <see cref="AwaitApprovalActivity"/>.</summary>
public class AwaitApprovalBookmark : IBookmark
{
    /// <summary>Creates a new instance of <see cref="AwaitApprovalBookmark"/>.</summary>
    public AwaitApprovalBookmark(string caseId, string role, bool blockPreviousApprover = false, IEnumerable<string>? publicActions = null) {
        CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
        Role = role;
        BlockPreviousApprover = blockPreviousApprover;
        PublicActions = publicActions ?? new List<string>();
    }

    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; }

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    [ExcludeFromHash]
    public string Role { get; set; }

    /// <summary>Block previous approver from triggering the bookmark.</summary>
    [ExcludeFromHash]
    public bool BlockPreviousApprover { get; set; }
    
    /// <summary>Public Actions Allowed.</summary>
    [ExcludeFromHash]
    public IEnumerable<string> PublicActions { get; set; }
}

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
        var publicActions = await context.ReadActivityPropertyAsync<AwaitApprovalActivity, IEnumerable<string>>(x => x.PublicActions, cancellationToken);
        return new[] {
            // Create a bookmark for the activity's input role (or "" if left blank (that means bookmark will be triggered by an authenticated-only user))
            Result(new AwaitApprovalBookmark(context.ActivityExecutionContext.CorrelationId, allowedRole, blockPreviousApprover, publicActions)),
            // Always create a bookmark for the administrator (also ignore blocking)
            // Result(new AwaitApprovalBookmark(context.ActivityExecutionContext.CorrelationId, Security.BasicRoleNames.Administrator)) 
        };
    }
}