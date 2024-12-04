namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>Models case details.</summary>
public class Case : CasePartial
{
    /// <summary>The attachments of the case.</summary>
    public List<CaseAttachment> Attachments { get; set; } = new();

    /// <summary>The back-office users that approved the case.</summary>
    public List<AuditMeta> Approvers { get; set; } = new();
}
