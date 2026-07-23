using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Base frame of the Dex message/delta streaming protocol, emitted over SSE while streaming a chat turn.
/// Document patches ride the <c>delta</c> SSE event as <see cref="DexChatStreamDelta"/> (JSON Pointer
/// path/op/value); lifecycle frames (<c>start</c>, <c>status</c>, <c>error</c>, <c>done</c>) ride the default
/// <c>message</c> event. All payloads carry the <c>type</c> discriminator; clients must ignore unknown types.
/// Applying the patch frames in order to an empty JSON document produces the serialized
/// <see cref="DexChatResponse"/> of the turn (modulo <c>null</c> members and the computed <c>text</c> member).
/// The stream is terminal after <see cref="DexChatStreamDone"/> (success) or <see cref="DexChatStreamError"/> (failure).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DexChatStreamStart), "start")]
[JsonDerivedType(typeof(DexChatStreamStatus), "status")]
[JsonDerivedType(typeof(DexChatStreamDelta), "delta")]
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

    /// <summary>Ephemeral guest credentials, present only when the session was created anonymously. The client must use the token as bearer on subsequent calls.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GuestSession? GuestSession { get; set; }
}

/// <summary>Pipeline progress label. Ephemeral streaming UI hint — never part of the assembled document.</summary>
public class DexChatStreamStatus : DexChatResponseUpdate
{
    /// <summary>Human-readable progress label (e.g. <c>Retrieving relevant context</c>).</summary>
    public required string Value { get; set; }
}

/// <summary>A document patch. Rides the <c>delta</c> SSE event; the client applies patches in arrival order to its copy of the response document.</summary>
public class DexChatStreamDelta : DexChatResponseUpdate
{
    /// <summary>JSON Pointer (RFC 6901) into the response document (e.g. <c>/messages/0/content/parts/0/value</c>). Omitted (null) ⇒ same as the previous <c>delta</c> frame's effective path (frame compaction). The first delta of a stream always carries it. Note: the empty string is the RFC 6901 root pointer, not inheritance.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    /// <summary>The operation to apply at <see cref="Path"/>. Omitted (null) ⇒ same as the previous <c>delta</c> frame's effective op (frame compaction). The first delta of a stream always carries it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DexChatPatchOp? Op { get; set; }

    /// <summary>Operand of the operation. Never inherited. Serialized by runtime type with the host's JSON options.</summary>
    public object? Value { get; set; }
}

/// <summary>Terminal failure frame. The reason is safe to display — exception details never reach the wire.</summary>
public class DexChatStreamError : DexChatResponseUpdate
{
    /// <summary>User-displayable reason for the failure.</summary>
    public required string Reason { get; set; }
}

/// <summary>Terminal success frame — a bare commit marker: the document is complete, render final. All response data (ids, usage, finish reason) has already arrived as patches.</summary>
public class DexChatStreamDone : DexChatResponseUpdate
{
}
