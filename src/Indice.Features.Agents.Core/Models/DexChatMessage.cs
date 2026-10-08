using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>A single chat message surfaced at the API boundary. Mirrors <see cref="ChatMessage"/> with parts-based content and first-class citations and sources.</summary>
public class DexChatMessage
{
    /// <summary>Identifier of the message. Matches the persisted message id; <see cref="Guid.Empty"/> when the turn was blocked and nothing was persisted.</summary>
    public string? MessageId { get; set; }

    /// <summary>Author name of the message.</summary>
    public string? AuthorName { get; set; }

    /// <summary>Author role of this message. Serializes as the role's lowercase value (e.g. <c>user</c>).</summary>
    public DexChatRole Role { get; set; } = DexChatRole.User;

    /// <summary>Message body as content parts.</summary>
    public ChatMessageContent Content { get; set; } = new();

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>User feedback on the assistant message: <c>true</c> liked, <c>false</c> disliked, <c>null</c> none.</summary>
    public bool? Liked { get; set; }

    /// <summary>Citations referenced by the message text, ordered by citation number.</summary>
    public List<Citation> Citations { get; set; } = [];

    /// <summary>Source documents backing the message. Reserved contract field — empty until the pipeline surfaces sources on the response.</summary>
    public List<SourceDocumentLink> Sources { get; set; } = [];

    /// <summary>Concatenated text of all content parts, like <see cref="ChatMessage.Text"/>. Convenience accessor; not serialized (the parts are).</summary>
    [JsonIgnore]
    public string Text => string.Concat(Content.Parts.Select(part => part.Value));
}
