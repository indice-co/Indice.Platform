using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AIChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Projections from the persisted chat message DTOs onto the framework (Microsoft.Extensions.AI) shapes the MAF pipeline consumes.</summary>
internal static class ChatMessageMappings
{
    /// <summary>Projects a persisted session turn into the framework message shape.</summary>
    public static AIChatMessage ToAIChatMessage(this ChatMessage message) => new(ToAIChatRole(message.Role), message.Content);

    /// <summary>Maps the persisted <see cref="ChatMessageRole"/> onto the framework role the MAF pipeline consumes.</summary>
    public static AIChatRole ToAIChatRole(ChatMessageRole role) => role switch {
        ChatMessageRole.User => AIChatRole.User,
        ChatMessageRole.Assistant => AIChatRole.Assistant,
        ChatMessageRole.System => AIChatRole.System,
        ChatMessageRole.Tool => AIChatRole.Tool,
        _ => AIChatRole.User,
    };
}
