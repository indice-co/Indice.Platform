namespace Indice.Features.Cases.Core.Models.Requests;
/// <summary>Request to add permission to a user</summary>
public class AddCaseAccessRuleRequest
{
    /// <summary>CheckpointTypeId that member will get permission.</summary>
    public Guid? RuleCheckpointTypeId { get; set; }
    /// <summary>Member Role that will have permission for the case.</summary>
    public string? MemberRole { get; set; }
    /// <summary>Group that will have permission for the case.</summary>
    public string? MemberGroupId { get; set; }
    /// <summary>User that will have  permission for the case.</summary>
    public string? MemberUserId { get; set; }
    /// <summary>Access level for member on the case.</summary>
    public int AccessLevel { get; set; }

    /// <summary>
    /// Validates the request, so that at least one of <see cref="MemberRole"/>, <see cref="MemberGroupId"/>,
    /// <see cref="MemberUserId"/> is specified. Whitespaces are allowed.
    /// </summary>
    public bool IsValid() =>
        !(string.IsNullOrEmpty(MemberRole) && string.IsNullOrEmpty(MemberGroupId) && string.IsNullOrEmpty(MemberUserId));
    
}