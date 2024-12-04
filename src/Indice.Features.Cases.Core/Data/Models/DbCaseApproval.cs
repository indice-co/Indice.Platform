using Indice.Features.Cases.Core.Models;
namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCaseApproval
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid? CommentId { get; set; }
    public AuditMeta CreatedBy { get; set; } = null!;
    public Approval Action { get; set; }
    /// <summary>An indicator whether the approval action is committed from user and the upcoming service. </summary>
    public bool Committed { get; set; }
    public string? Reason { get; set; }
    public virtual DbCase Case { get; set; } = null!;
    public virtual DbComment Comment { get; set; } = null!;
}
#pragma warning restore 1591
