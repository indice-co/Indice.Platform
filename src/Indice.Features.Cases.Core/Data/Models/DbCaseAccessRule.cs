namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCaseAccessRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? RuleCaseId { get; set; }
    public Guid? RuleCaseTypeId { get; set; }
    public Guid? RuleCheckpointTypeId { get; set; }


    public string MemberRole { get; set; }
    public string MemberGroupId { get; set; }
    public string MemberUserId { get; set; }

    public int AccessLevel { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;

    public virtual DbCaseType CaseType { get; set; }
    public virtual DbCheckpointType CheckpointType { get; set; }
    public virtual DbCase Case { get; set; }
}
#pragma warning restore 1591
