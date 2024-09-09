using System;

namespace Indice.Features.Cases.Data.Models;

#pragma warning disable 1591
public class DbCaseMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? RuleCaseTypeId { get; set; }
    public Guid? RuleCheckpointTypeId { get; set; }
    public Guid? RuleCaseId { get; set; }


    public string MemberRole { get; set; }
    public string MemberGroupId { get; set; }
    public string MemberUserId { get; set; }

    public int AccessLevel { get; set; }

    public virtual DbCaseType CaseType { get; set; }
    public virtual DbCheckpointType CheckpointType { get; set; }
    public virtual DbCase Case { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;
}
#pragma warning restore 1591
