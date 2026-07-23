using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Projects a chat turn into the JSON Pointer patch frames of the streaming protocol (<see cref="DexChatStreamDelta"/>).
/// One-way outbound projection — persistence still flows through the pipeline's aggregated <c>ChatResponse</c>. The frames
/// emitted for a turn, applied in order to an empty JSON document, produce the serialized <see cref="DexChatResponse"/>
/// of the turn (modulo <c>null</c> members and the computed <c>text</c> member). Stateful per turn: tracks the open
/// content part so token appends target the right pointer; not thread-safe, one instance per stream.
/// </summary>
public class DexChatStreamProjector
{
    private const string TextContentType = "text/markdown";
    private bool _messageOpened;
    private int _partIndex = -1;
    private string? _openTextContentType;

    /// <summary>Emits the conversation identity patch. Call once, first.</summary>
    public IEnumerable<DexChatStreamDelta> Begin(Guid conversationId) {
        yield return Add("/conversationId", conversationId);
    }

    /// <summary>Appends streamed answer text: opens the message skeleton and a text part on first use (or after an atomic part), then appends to the open part.</summary>
    public IEnumerable<DexChatStreamDelta> AppendText(string text, string contentType = TextContentType) {
        foreach (var frame in EnsureMessage()) { yield return frame; }
        if (_openTextContentType != contentType) {
            _partIndex++;
            _openTextContentType = contentType;
            yield return Add("/messages/0/content/parts/-", ChatMessagePart.FromText(string.Empty, contentType));
        }
        yield return new DexChatStreamDelta { Op = DexChatPatchOp.Append, Path = $"/messages/0/content/parts/{_partIndex}/value", Value = text };
    }

    /// <summary>Adds an atomic (non-streamed) content part — images, embedded data. Closes any open text part; the next text opens a new one.</summary>
    public IEnumerable<DexChatStreamDelta> AddPart(ChatMessagePart part) {
        foreach (var frame in EnsureMessage()) { yield return frame; }
        _partIndex++;
        _openTextContentType = null;
        yield return Add("/messages/0/content/parts/-", part);
    }

    /// <summary>Emits the completion tail for a streamed turn: the assistant message's non-null metadata (persisted id, role, timestamps, citations, sources) and the non-null root members. The parts themselves were already streamed.</summary>
    public IEnumerable<DexChatStreamDelta> Complete(DexChatResponse response) {
        if (response.Messages.LastOrDefault() is { } message) {
            foreach (var frame in EnsureMessage()) { yield return frame; }
            if (message.MessageId is not null) { yield return Add("/messages/0/messageId", message.MessageId); }
            if (message.AuthorName is not null) { yield return Add("/messages/0/authorName", message.AuthorName); }
            yield return Add("/messages/0/role", message.Role);
            if (message.CreatedAt is not null) { yield return Add("/messages/0/createdAt", message.CreatedAt); }
            if (message.Citations.Count > 0) { yield return Add("/messages/0/citations", message.Citations); }
            if (message.Sources.Count > 0) { yield return Add("/messages/0/sources", message.Sources); }
        }
        foreach (var frame in RootTail(response)) { yield return frame; }
    }

    /// <summary>Projects an already-complete, never-streamed response (e.g. the limit-blocked reply) as a full patch sequence: identity, the complete messages array in one operation, then the root tail.</summary>
    public IEnumerable<DexChatStreamDelta> ProjectResponse(DexChatResponse response) {
        if (response.ConversationId is { } conversationId) {
            foreach (var frame in Begin(conversationId)) { yield return frame; }
        }
        _messageOpened = true;
        yield return Add("/messages", response.Messages);
        foreach (var frame in RootTail(response)) { yield return frame; }
    }

    private IEnumerable<DexChatStreamDelta> EnsureMessage() {
        if (_messageOpened) { yield break; }
        _messageOpened = true;
        yield return Add("/messages", new List<DexChatMessage> { new() { Role = DexChatRole.Assistant, Content = new ChatMessageContent() } });
    }

    private static IEnumerable<DexChatStreamDelta> RootTail(DexChatResponse response) {
        if (response.ResponseId is not null) { yield return Add("/responseId", response.ResponseId); }
        if (response.ModelId is not null) { yield return Add("/modelId", response.ModelId); }
        if (response.CreatedAt is not null) { yield return Add("/createdAt", response.CreatedAt); }
        if (response.FinishReason is not null) { yield return Add("/finishReason", response.FinishReason); }
        if (response.Usage is not null) { yield return Add("/usage", response.Usage); }
        yield return Add("/limitReached", response.LimitReached);
    }

    private static DexChatStreamDelta Add(string path, object? value) => new() { Op = DexChatPatchOp.Add, Path = path, Value = value };
}
