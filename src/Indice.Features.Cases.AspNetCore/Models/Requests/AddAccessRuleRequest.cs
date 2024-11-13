namespace Indice.Features.Cases.Models.Requests;
/// <summary>Request to add permission to a user</summary>
public class AddAccessRuleRequest
{
    /// <summary>CaseId that member will get permission.</summary>
    public Guid? RuleCaseId { get; set; }
    /// <summary>CheckpointTypeId that member will get permission.</summary>
    public Guid? RuleCheckpointTypeId { get; set; }
    /// <summary>CaseTypeId that member will get permission.</summary>
    public Guid? RuleCaseTypeId { get; set; }

    /// <summary>Member Role that will have permission.</summary>
    public string MemberRole { get; set; }
    /// <summary>Group that will have permission.</summary>
    public string MemberGroupId { get; set; }
    /// <summary>User that will have  permission.</summary>
    public string MemberUserId { get; set; }

    /// <summary>Access level for member on the resource.</summary>
    public int AccessLevel { get; set; }

    public bool IsValid() => 
            //Validate Rules
            (
                (RuleCaseTypeId != null && RuleCheckpointTypeId == null && RuleCaseId == null) ||
                (RuleCaseTypeId == null && RuleCheckpointTypeId != null && RuleCaseId == null) ||
                (RuleCaseTypeId == null && RuleCheckpointTypeId == null && RuleCaseId != null) ||
                (RuleCaseTypeId == null && RuleCheckpointTypeId != null && RuleCaseId != null)
            )
            &&
            //Validate Members
            (
             (!string.IsNullOrEmpty(MemberRole) && string.IsNullOrEmpty(MemberGroupId) && string.IsNullOrEmpty(MemberUserId)) ||
             (string.IsNullOrEmpty(MemberRole) && !string.IsNullOrEmpty(MemberGroupId) && string.IsNullOrEmpty(MemberUserId)) ||
             (string.IsNullOrEmpty(MemberRole) && string.IsNullOrEmpty(MemberGroupId) && !string.IsNullOrEmpty(MemberUserId))
            );
}
