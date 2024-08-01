using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Models;

/// <summary>The request to insert a new message to a case.</summary>
internal class SendMessageRequest
{
    /// <summary>The Id of the Checkpoint the message is replying to.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Indicates if the comment should be visible to the customer.</summary>
    public bool? PrivateComment { get; set; } = true;

    /// <summary>The comment to add to the checkpoint.</summary>
    public string Comment { get; set; }

}