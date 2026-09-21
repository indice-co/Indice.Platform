using System.Net.Mime;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Body accepted by both <c>POST /api/my/chats</c> (creates the session inline) and <c>POST /api/my/chats/{id}/messages</c>.</summary>
public class ChatRequest
{
    /// <summary>The end-user message text.</summary>
    public string? Text {
        get { return Parts.FirstOrDefault(x => x.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase))?.Value; }
        set {
            var textPrompt = Parts.FirstOrDefault(x => x.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase));
            if (textPrompt is null) {
                Parts.Add(ChatMessagePart.FromText(value ?? string.Empty, MediaTypeNames.Text.Plain, "Prompt"));
            } else {
                textPrompt.Value = value ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// The structured content of the message.
    /// </summary>
    public List<ChatMessagePart> Parts { get; init; } = [ChatMessagePart.FromText(string.Empty, MediaTypeNames.Text.Plain, "Prompt")];

    /// <summary>Optional display name of the end-user. If not provided, the system will use a default name.</summary>
    public string? AuthorName { get; set; }

    /// <summary>Optional name of the agent to use for this chat. If not provided, the system will use a default agent.</summary>
    public string? AgentName { get; set; }

    /// <summary>Optional topic of the conversation. it grounds the workflow so it can skip discovering the purpose of the visit.</summary>
    public ChatTopic? Topic { get; init; }
}