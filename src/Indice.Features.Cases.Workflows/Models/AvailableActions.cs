using Indice.Features.Cases.Workflows.Bookmarks;

namespace Indice.Features.Cases.Workflows.Models;

public class AvailableActions
{
    public List<AwaitAssignmentBookmark?>? AssignmentBookmarks { get; set; }
    
    public List<AwaitEditBookmark?>? EditBookmarks { get; set; }
    
    public List<AwaitApprovalBookmark?>? ApprovalBookmarks { get; set; }
    
    public  List<WorkflowCustomCaseAction>? CustomCaseActions { get; set; }
}