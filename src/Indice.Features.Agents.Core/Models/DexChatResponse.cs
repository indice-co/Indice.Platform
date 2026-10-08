using Microsoft.Extensions.AI;
using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Response of a non-streaming chat turn at the API boundary. Mirrors <see cref="ChatResponse"/> and promotes product fields (question counters, limit flag, citations, sources) to first-class members.</summary>
public class DexChatResponse
{
    /// <summary>Identifier of the conversation this turn belongs to.</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>Identifier of the response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Messages composing the response — typically a single assistant message.</summary>
    public List<DexChatMessage> Messages { get; set; } = [];

    /// <summary>Identifier of the model that produced the answer.</summary>
    public string? ModelId { get; set; }

    /// <summary>Creation timestamp of the response.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Reason generation stopped. Serializes as its string value (e.g. <c>stop</c>; <c>limit</c> for blocked turns).</summary>
    public DexChatFinishReason? FinishReason { get; set; }

    /// <summary>Token usage for the turn.</summary>
    public DexChatUsage? Usage { get; set; }

    /// <summary>True when the turn was blocked by a session usage limit — <see cref="Text"/> carries the predefined limit message and nothing was persisted.</summary>
    public bool LimitReached { get; set; }

    /// <summary>Ephemeral guest credentials, present only when the session was created anonymously. The client must use the token as bearer on subsequent calls.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GuestSession? GuestSession { get; set; }

    /// <summary>Concatenated text of all messages, like <see cref="ChatResponse.Text"/>. Serialized as a convenience for consumers.</summary>
    public string Text => string.Concat(Messages.Select(message => message.Text));
}
