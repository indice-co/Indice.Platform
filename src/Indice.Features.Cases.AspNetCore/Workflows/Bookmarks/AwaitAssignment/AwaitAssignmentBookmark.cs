using Elsa.Attributes;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment;

/// <summary>Bookmark model for <see cref="AwaitAssignmentActivity"/>.</summary>
public class AwaitAssignmentBookmark : IBookmark
{
    /// <summary>Create a new <see cref="AwaitAssignmentBookmark"/> bookmark.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="role">The role to create the bookmark for.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AwaitAssignmentBookmark(string caseId, string role, bool assign = false, bool selfAssign = false) {
        Role = role;
        CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
        Assign = assign;
        SelfAssign = selfAssign;
    }

    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; }

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    public string Role { get; set; }
    [ExcludeFromHash]
    public bool Assign { get; set; }
    [ExcludeFromHash]
    public bool SelfAssign { get; set; }
}