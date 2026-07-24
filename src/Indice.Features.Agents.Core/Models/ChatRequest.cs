namespace Indice.Features.Agents.Core.Models;

/// <summary>Body accepted by both <c>POST /api/my/chats</c> (creates the session inline) and <c>POST /api/my/chats/{id}/messages</c>.</summary>
public class ChatRequest
{
    /// <summary>The end-user message text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Optional display name of the end-user. If not provided, the system will use a default name.</summary>
    public string? AuthorName { get; set; }
}