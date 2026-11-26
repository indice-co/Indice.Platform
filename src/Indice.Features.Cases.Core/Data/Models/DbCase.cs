using System.Linq.Expressions;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCase
{
    public Guid Id { get; set; }
    public Guid CaseTypeId { get; set; }
    public Priority Priority { get; set; }
    /// <summary>case belongs to this customer, regardless of whether he created it or not</summary>
    public ContactMeta Owner { get; set; } = new ();
    public string? GroupId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public Guid? PublicCheckpointId { get; set; }
    public Guid? CheckpointId { get; set; }
    public Guid? PublicDataId { get; set; }
    public Guid? DataId { get; set; }
    public AuditMeta CreatedBy { get; set; } = null!;
    public AuditMeta? CompletedBy { get; set; }
    public AuditMeta? AssignedTo { get; set; }
    public bool Draft { get; set; }
    public string? Channel { get; set; }
    public virtual DbCaseType CaseType { get; set; } = null!;
    public virtual DbCheckpoint PublicCheckpoint { get; set; } = null!;
    public virtual DbCheckpoint Checkpoint { get; set; } = null!;
    public virtual DbCaseData PublicData { get; set; } = null!;
    public virtual DbCaseData Data { get; set; } = null!;
    public virtual List<DbAttachment> Attachments { get; set; } = [];
    public virtual List<DbCheckpoint> Checkpoints { get; set; } = [];
    public virtual List<DbComment> Comments { get; set; } = [];
    public virtual List<DbCaseData> Versions { get; set; } = [];
    public virtual List<DbCaseApproval> Approvals { get; set; } = [];
    public string? ReferenceNumber { get; set; }
    /// <summary>Correlates cases. Mirrors the <c>ExternalCorrelationKey</c> metadata entry.</summary>
    public string? RelationKey { get; set; }
    /// <summary>The coowners of the case</summary>
    public virtual List<DbCoownsCase> CoOwners { get; set; } = [];

    /// <summary>The permission of the user that created a case, when that user is neither its owner nor one of its co-owners.</summary>
    /// <remarks>The creator can view and work on the case, but only an owner can discard it.</remarks>
    public const ContactPermission CreatorPermission = ContactPermission.Contributor;

    /// <summary>
    /// Matches the cases on which a customer has at least the given permission.
    /// The owner is judged by the permission of the owner, a co-owner by the permission of the co-owner,
    /// and the creator that is neither gets <see cref="CreatorPermission"/>.
    /// </summary>
    /// <param name="userId">The id of the customer.</param>
    /// <param name="minimumPermission">The minimum permission required.</param>
    public static Expression<Func<DbCase, bool>> ContactAccessPredicate(string? userId, ContactPermission minimumPermission) {
        if (string.IsNullOrEmpty(userId)) {
            return @case => false;
        }
        if (minimumPermission > CreatorPermission) {
            return @case =>
                (@case.Owner.UserId == userId && @case.Owner.Permission >= minimumPermission) ||
                @case.CoOwners.Any(x => x.CoOwner.UserId == userId && x.CoOwner.Permission >= minimumPermission);
        }
        return @case =>
            (@case.Owner.UserId == userId && @case.Owner.Permission >= minimumPermission) ||
            @case.CoOwners.Any(x => x.CoOwner.UserId == userId && x.CoOwner.Permission >= minimumPermission) ||
            (@case.CreatedBy.Id == userId && @case.Owner.UserId != userId && !@case.CoOwners.Any(x => x.CoOwner.UserId == userId));
    }
}
#pragma warning restore 1591
