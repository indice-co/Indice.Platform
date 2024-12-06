namespace Indice.Features.Cases.Core.Models;

/// <summary>The request to insert a new comment to a case.</summary>
public class SendCommentRequest
{
    /// <summary>The Id of the Checkpoint the comment is replying to.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Indicates if the comment should be visible to the customer.</summary>
    public bool? PrivateComment { get; set; } = true;

    /// <summary>The comment to add to the checkpoint.</summary>
    public string? Comment { get; set; }

}