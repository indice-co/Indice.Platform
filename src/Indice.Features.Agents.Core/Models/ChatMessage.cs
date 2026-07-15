using OpenAI.Assistants;

namespace Indice.Features.Agents.Core.Models;

/// <summary>A single turn (user or assistant) in a chat session. DTO exposed at the service boundary; mirrors <see cref="Data.DbMessage"/>.</summary>
public class ChatMessage
{
    /// <summary>Message identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Author role of this message. Serializes as the role's lowercase value (e.g. <c>user</c>).</summary>
    public ChatMessageRole Role { get; init; } = ChatMessageRole.User;

    /// <summary>Message body.</summary>
    public ChatMessageContent Content { get; init; } = new();

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>References to chunks.</summary>
    public List<Citation> Citations { get; set; } = [];
    
    /// <summary>References to source documents.</summary>
    public List<SourceDocumentLink> Sources { get; set; } = [];
}
