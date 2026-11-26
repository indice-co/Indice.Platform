namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>Models case details.</summary>
public class Case : CasePartial
{
    /// <summary>The attachments of the case.</summary>
    public List<CaseAttachment> Attachments { get; set; } = new();

    /// <summary>The back-office users that approved the case.</summary>
    public List<AuditMeta> Approvers { get; set; } = new();

    /// <summary>The permission of the owner on the case.</summary>
    public ContactPermission? OwnerPermission { get; set; }

    /// <summary>The co-owners of the case. Filled only by the back-office case details endpoint.</summary>
    public List<CaseCoOwner>? CoOwners { get; set; }
}
