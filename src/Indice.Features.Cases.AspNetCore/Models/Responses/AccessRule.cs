namespace Indice.Features.Cases.Models.Responses;

/// <summary>Access rule model</summary>
public class AccessRule
{
    /// <summary>Rule record Id.</summary>
    public Guid? Id { get; set; }
    /// <summary>CaseId that member will get permission.</summary>
    public Guid? RuleCaseId { get; set; }
    /// <summary>CaseTypeId that member will get permission.</summary>
    public Guid? RuleCaseTypeId { get; set; }
    /// <summary>CheckpointTypeId that member will get permission.</summary>
    public Guid? RuleCheckpointTypeId { get; set; }

    /// <summary>Member Role that will have permission.</summary>
    public string? MemberRole { get; set; }
    /// <summary>Group that will have permission.</summary>
    public string? MemberGroupId { get; set; }
    /// <summary>User that will have  permission.</summary>
    public string? MemberUserId { get; set; }

    /// <summary>Access level for member on the resource.</summary>
    public int AccessLevel { get; set; }
}