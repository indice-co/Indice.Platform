using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment;

/// <summary>Bookmark model for <see cref="AwaitAssignmentActivity"/>.</summary>
public class AwaitAssignmentVol2Bookmark : IBookmark
{
    /// <summary>Create a new <see cref="AwaitAssignmentVol2Bookmark"/> bookmark.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="role">The role to create the bookmark for.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AwaitAssignmentVol2Bookmark(string caseId, string role) {
        Role = role;
        CaseId = string.IsNullOrEmpty(caseId) ? throw new ArgumentNullException(nameof(caseId), "CaseId cannot be null or empty.") : caseId;
    }

    /// <summary>The Id of the case to create the bookmark.</summary>
    public string CaseId { get; set; }

    /// <summary>The user role that can trigger the bookmark. Can be null for all authenticated users</summary>
    public string Role { get; set; }
}