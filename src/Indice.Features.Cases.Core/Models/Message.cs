namespace Indice.Features.Cases.Core.Models;

/// <summary>
/// The message model that creates a new checkpoint for the case, or adds a simple comment, attaches a file
/// or replies to other comment. 
/// </summary>
public class Message
{
    /// <summary>The Id of the Checkpoint the message is replying to.</summary>
    public Guid? ReplyToCommentId { get; set; }
    
    /// <summary>The name of the checkpoint the case must proceed.</summary>
    public string? CheckpointTypeName { get; set; }
    
    /// <summary>Indicates if the comment should be visible to the customer.</summary>
    public bool? PrivateComment { get; set; } = true;
    
    /// <summary>The comment to add to the checkpoint.</summary>
    public string? Comment { get; set; }
    
    /// <summary>The file that is attached with the checkpoint.</summary>
    public Func<Stream>? FileStreamAccessor { get; set; }
    /// <summary>The file name that is attached with the checkpoint.</summary>
    public string? FileName { get; set; }

    /// <summary>The data related with the message.</summary>
    public dynamic? Data { get; set; }
}