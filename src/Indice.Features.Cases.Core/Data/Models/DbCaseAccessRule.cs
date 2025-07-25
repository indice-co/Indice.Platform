using System.Linq.Expressions;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCaseAccessRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? RuleCaseId { get; set; }
    public Guid? RuleCaseTypeId { get; set; }
    public Guid? RuleCheckpointTypeId { get; set; }


    public string? MemberRole { get; set; }
    public string? MemberGroupId { get; set; }
    public string? MemberUserId { get; set; }

    public int AccessLevel { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;

    public virtual DbCaseType? CaseType { get; set; }
    public virtual DbCheckpointType? CheckpointType { get; set; }
    public virtual DbCase? Case { get; set; }

    public static Expression<Func<DbCaseAccessRule, bool>> AccessMatchPredicate(string? userId, List<string> userRoles, string? groupId) {
        return x =>
            (userId != null && x.MemberUserId == userId) ||
            (userRoles.Any() && userRoles.Contains(x.MemberRole!)) ||
            (groupId != null && x.MemberGroupId == groupId);
    }
    public static Expression<Func<DbCaseAccessRule, bool>> RuleMatchPredicate(Guid caseId, Guid caseTypeId, Guid checkpointTypeId) {
        return x =>
                        (x.RuleCaseId == caseId && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == null) ||
                        (x.RuleCaseId == null && x.RuleCaseTypeId == caseTypeId && x.RuleCheckpointTypeId == null) ||
                        (x.RuleCaseId == null && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpointTypeId) ||
                        (x.RuleCaseId == caseId && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpointTypeId);
    }
}
#pragma warning restore 1591
