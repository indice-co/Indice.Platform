using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Indice.Features.Agents.Core.Models;

/// <summary>A single turn (user or assistant) in a chat session. DTO exposed at the service boundary; mirrors <see cref="Data.DbSessionMessage"/>.</summary>
public class ChatMessage
{
    /// <summary>Message identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Author role of this message. Serializes as the role's lowercase value (e.g. <c>user</c>).</summary>
    public ChatRole Role { get; init; } = ChatRole.User;

    /// <summary>Message body.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
