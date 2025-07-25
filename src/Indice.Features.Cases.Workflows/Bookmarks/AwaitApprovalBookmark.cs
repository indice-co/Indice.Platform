using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Services;

namespace Indice.Features.Cases.Workflows.Bookmarks;

/// <summary>Bookmark model for <see cref="AwaitApprovalActivity"/>. This model will be persisted by Elsa.</summary>
public class AwaitApprovalBookmark(string caseId, string? role = null, bool blockPreviousApprover = false, IEnumerable<string>? publicActions = null) : IBookmark
{
    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; } = !string.IsNullOrEmpty(caseId) ? caseId : throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.");

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    [ExcludeFromHash]
    public string Role { get; set; } = role ?? string.Empty;

    /// <summary>Block previous approver from triggering the bookmark.</summary>
    [ExcludeFromHash]
    public bool BlockPreviousApprover { get; set; } = blockPreviousApprover;
    
    /// <summary>Public Actions Allowed.</summary>
    [ExcludeFromHash]
    public IEnumerable<string> PublicActions { get; set; } = publicActions ?? new List<string>();
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
        return [
            Result(new AwaitApprovalBookmark(context.ActivityExecutionContext.CorrelationId, allowedRole, blockPreviousApprover, publicActions))
        ];
    }
}