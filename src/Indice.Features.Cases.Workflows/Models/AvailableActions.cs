using Indice.Features.Cases.Workflows.Bookmarks;

namespace Indice.Features.Cases.Workflows.Models;

/// <summary>
/// A list of available actions for the current caseId.
/// These are custom blocking activities in the Elsa Context.
/// </summary>
public class AvailableActions
{
    /// <summary>List of current Assignment Bookmarks.</summary>
    public List<AwaitAssignmentBookmark?>? AssignmentBookmarks { get; set; }
    
    /// <summary>List of current Edit Bookmarks.</summary>
    public List<AwaitEditBookmark?>? EditBookmarks { get; set; }
    
    /// <summary>List of current Approval Bookmarks.</summary>
    public List<AwaitApprovalBookmark?>? ApprovalBookmarks { get; set; }
    
    /// <summary>List of current Custom Actions.</summary>
    public List<CustomAction>? CustomActions { get; set; }
}