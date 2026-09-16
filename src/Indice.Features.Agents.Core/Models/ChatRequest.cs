using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Body accepted by both <c>POST /api/my/chats</c> (creates the session inline) and <c>POST /api/my/chats/{id}/messages</c>.</summary>
public class ChatRequest
{
    /// <summary>The end-user message text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// The end-user message content parts. Each part can be a text, an image, or any other supported content type. The system will process the parts in order and generate a response based on the combined content.
    /// </summary>
    public List<ChatMessagePart> Content { get; init; } = new List<ChatMessagePart>();

    /// <summary>Optional display name of the end-user. If not provided, the system will use a default name.</summary>
    public string? AuthorName { get; set; }

    /// <summary>Optional name of the agent to use for this chat. If not provided, the system will use a default agent.</summary>
    public string? AgentName { get; set; }

    /// <summary>Optional topic of the conversation. it grounds the workflow so it can skip discovering the purpose of the visit.</summary>
    public ChatTopic? Topic { get; init; }
}