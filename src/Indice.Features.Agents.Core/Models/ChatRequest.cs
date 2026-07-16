namespace Indice.Features.Agents.Core.Models;

/// <summary>Body accepted by both <c>POST /api/my/chats</c> (creates the session inline) and <c>POST /api/my/chats/{id}/messages</c>.</summary>
public class ChatRequest
{
    /// <summary>The end-user message text.</summary>
    public string Text { get; init; } = string.Empty;
}