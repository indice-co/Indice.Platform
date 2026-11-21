namespace Indice.Features.Agents.Core.Data;

/// <summary>
/// A file an agent produced during a conversation, such as a generated report. It belongs to the conversation (and is
/// deleted with it) and is only ever served back to the user who owns the conversation.
/// </summary>
public class DbFile
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key → <see cref="DbConversation.Id"/>. Files are deleted with their conversation.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>The owner: the user the conversation belongs to. Checked on every read, in addition to the conversation id.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The name the file is downloaded as.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The media type of the file.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The size in bytes.</summary>
    public long Length { get; set; }

    /// <summary>Lower-case hex SHA-256 of <see cref="Data"/>, computed when the file was saved. It lets a reader verify the file was not altered after it was handed over.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>The file content.</summary>
    public byte[] Data { get; set; } = [];

    /// <summary>When the file was saved.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
