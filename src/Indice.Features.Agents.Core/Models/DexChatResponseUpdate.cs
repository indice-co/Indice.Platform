using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Base frame of the Dex message/delta streaming protocol, emitted over SSE while streaming a chat turn.
/// Answer text rides the <c>delta</c> SSE event as <see cref="DexChatStreamDelta"/>; every other frame rides the
/// default <c>message</c> event and is one of the derived message types. All payloads carry the <c>type</c>
/// discriminator. The stream is terminal after <see cref="DexChatStreamDone"/> (success) or
/// <see cref="DexChatStreamError"/> (failure); the discrete part frames (citations, sources, usage) plus the
/// <c>done</c> metadata let the client assemble the equivalent of the non-streaming <see cref="DexChatResponse"/>
/// without the text being re-sent.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DexChatStreamStart), "start")]
[JsonDerivedType(typeof(DexChatStreamStatus), "status")]
[JsonDerivedType(typeof(DexChatStreamDelta), "delta")]
[JsonDerivedType(typeof(DexChatStreamCitations), "citations")]
[JsonDerivedType(typeof(DexChatStreamSources), "sources")]
[JsonDerivedType(typeof(DexChatStreamUsage), "usage")]
[JsonDerivedType(typeof(DexChatStreamError), "error")]
[JsonDerivedType(typeof(DexChatStreamDone), "done")]
public abstract class DexChatResponseUpdate
{
}

/// <summary>First frame of every stream: identifies the conversation the turn belongs to (new or existing).</summary>
public class DexChatStreamStart : DexChatResponseUpdate
{
    /// <summary>Identifier of the conversation this turn belongs to.</summary>
    public Guid ConversationId { get; set; }
}

/// <summary>Pipeline progress label. Ephemeral streaming UI hint — never part of the assembled answer.</summary>
public class DexChatStreamStatus : DexChatResponseUpdate
{
    /// <summary>Human-readable progress label (e.g. <c>Retrieving relevant context</c>).</summary>
    public required string Value { get; set; }
}

/// <summary>A chunk of answer text. Rides the <c>delta</c> SSE event; the client appends chunks in arrival order.</summary>
public class DexChatStreamDelta : DexChatResponseUpdate
{
    /// <summary>Text to append to the assistant message.</summary>
    public required string Text { get; set; }
}

/// <summary>Citations referenced by the completed answer, ordered by citation number. Emitted only when non-empty.</summary>
public class DexChatStreamCitations : DexChatResponseUpdate
{
    /// <summary>The citations of the assistant message.</summary>
    public required List<Citation> Value { get; set; }
}

/// <summary>Source documents backing the completed answer. Emitted only when non-empty (reserved — the pipeline does not surface sources yet).</summary>
public class DexChatStreamSources : DexChatResponseUpdate
{
    /// <summary>The source documents of the assistant message.</summary>
    public required List<SourceDocumentLink> Value { get; set; }
}

/// <summary>Token and question usage for the turn, as on <see cref="DexChatResponse.Usage"/>.</summary>
public class DexChatStreamUsage : DexChatResponseUpdate
{
    /// <summary>The usage counters of the turn.</summary>
    public required DexChatUsage Value { get; set; }
}

/// <summary>Terminal failure frame. The reason is safe to display — exception details never reach the wire.</summary>
public class DexChatStreamError : DexChatResponseUpdate
{
    /// <summary>User-displayable reason for the failure.</summary>
    public required string Reason { get; set; }
}

/// <summary>
/// Terminal success frame carrying the response metadata that is not streamed as text: the persisted assistant
/// message id, response/model identifiers, timestamp, finish reason and the limit flag.
/// </summary>
public class DexChatStreamDone : DexChatResponseUpdate
{
    /// <summary>Identifier of the persisted assistant message; <see cref="Guid.Empty"/> when the turn was blocked and nothing was persisted.</summary>
    public string? MessageId { get; set; }

    /// <summary>Identifier of the response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Identifier of the model that produced the answer.</summary>
    public string? ModelId { get; set; }

    /// <summary>Creation timestamp of the response.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Reason generation stopped (<c>stop</c>; <c>limit</c> for blocked turns).</summary>
    public DexChatFinishReason? FinishReason { get; set; }

    /// <summary>True when the turn was blocked by a session usage limit — the streamed text carried the predefined limit message and nothing was persisted.</summary>
    public bool LimitReached { get; set; }
}
