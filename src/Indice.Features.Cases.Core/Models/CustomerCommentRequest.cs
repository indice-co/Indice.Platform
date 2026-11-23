namespace Indice.Features.Cases.Core.Models;

/// <summary>A public customer comment or reply.</summary>
public class CustomerCommentRequest
{
    /// <summary>The comment text.</summary>
    public string? Comment { get; set; }
    /// <summary>The optional public comment being replied to.</summary>
    public Guid? ReplyToCommentId { get; set; }
    /// <summary>The optional existing attachment belonging to this case.</summary>
    public Guid? AttachmentId { get; set; }
}
