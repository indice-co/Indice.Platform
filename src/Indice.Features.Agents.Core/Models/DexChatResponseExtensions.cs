using System.Text;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Conversions between the <c>Microsoft.Extensions.AI</c> chat types and the Dex boundary DTOs.</summary>
public static class DexChatResponseExtensions
{
    /// <summary>
    /// Maps a <see cref="ChatResponse"/> to the boundary <see cref="DexChatResponse"/>. Citations are lifted from the
    /// <see cref="CitationAnnotation"/>s the pipeline attaches to the message contents. <see cref="DexChatMessage.Sources"/>
    /// stays empty — sources are not carried by <see cref="ChatResponse"/>. Product counters (questions used/limit) and
    /// <see cref="DexChatResponse.LimitReached"/> are the caller's to set.
    /// </summary>
    public static DexChatResponse ToDexChatResponse(this ChatResponse response) {
        var messages = response.Messages.Select(message => message.ToDexChatMessage()).ToList();
        return new DexChatResponse {
            ConversationId = Guid.TryParse(response.ConversationId, out var conversationId) ? conversationId : null,
            ResponseId = response.ResponseId,
            Messages = messages,
            ModelId = response.ModelId,
            CreatedAt = response.CreatedAt,
            FinishReason = response.FinishReason.ToDexChatFinishReason(),
            Usage = response.Usage.ToDexChatUsage(),
        };
    }

    /// <summary>
    /// Maps a <see cref="ChatMessage"/> to the boundary <see cref="DexChatMessage"/>: adjacent non-empty textual
    /// contents merge into a single <c>text/markdown</c> <see cref="ChatMessagePart"/> (a data part closes the open
    /// text part), and <see cref="CitationAnnotation"/>s lift to <see cref="Citation"/>s (deduplicated per cited
    /// chunk — annotations carry one entry per marker occurrence). Mirrors the streaming projector, so the patched
    /// document and the non-streaming response share one canonical parts shape.
    /// </summary>
    public static DexChatMessage ToDexChatMessage(this ChatMessage message) {
        var content = new ChatMessageContent();
        var citations = new List<Citation>();
        ChatMessagePart? openTextPart = null;
        foreach (var item in message.Contents) {
            switch(item) {
                case TextContent text:
                    if (!string.IsNullOrEmpty(text.Text)) {
                        if (openTextPart is null) {
                            openTextPart = ChatMessagePart.FromText(text.Text, "text/markdown");
                            content.Parts.Add(openTextPart);
                        } else {
                            openTextPart.Value += text.Text;
                        }
                    }
                    // Annotations lift even from empty carriers — the pipeline emits citations on a trailing
                    // annotations-only empty text content once the full answer (and exact offsets) are known.
                    citations.AddRange((text.Annotations ?? []).OfType<CitationAnnotation>().Select(ToCitation));
                    break;
                case DataContent data:
                    content.Parts.Add(data.ToChatMessagePart());
                    openTextPart = null;
                    break;
                case UriContent uri:
                    content.Parts.Add(uri.ToChatMessagePart());
                    openTextPart = null;
                    break;
            }
        }
        var distinctCitations = citations.DistinctBy(citation => citation.ChunkId).OrderBy(citation => citation.Number).ToList();
        // Round-tripped annotations (persisted history) lose RawRepresentation and with it the citation Number —
        // stamp 1..n in appearance order so the markers stay renderable.
        if (distinctCitations.Count > 0 && distinctCitations.All(citation => citation.Number == 0)) {
            for (var i = 0; i < distinctCitations.Count; i++) {
                distinctCitations[i].Number = i + 1;
            }
        }
        return new DexChatMessage {
            MessageId = message.MessageId,
            AuthorName = message.AuthorName,
            Role = message.Role.ToDexChatRole(),
            Content = content,
            CreatedAt = message.CreatedAt,
            Liked = message.AdditionalProperties?.TryGetValue(nameof(DexChatMessage.Liked), out var liked) is true ? liked as bool? : null,
            Citations = distinctCitations
        };
    }

    /// <summary>
    /// Projects a <see cref="DataContent"/> into a boundary <see cref="ChatMessagePart"/>. A JSON payload (media type
    /// ending in <c>+json</c>, e.g. <see cref="AgentsConstants.MediaTypes.MultipleChoice"/>) carries its decoded UTF-8
    /// text so the client can parse it directly; anything else (images, embedded binaries) carries the base64
    /// <c>data:</c> URI. <see cref="DataContent.Name"/> lifts to <see cref="ChatMessagePart.Name"/>, which is how a bare
    /// <c>image/*</c> part carries its caption without the <see cref="AgentsConstants.MediaTypes.Image"/> envelope.
    /// Shared by the streaming projection (<c>ChatsService.StreamTurnAsync</c>) and the aggregated one
    /// (<see cref="ToDexChatMessage"/>) so the two cannot drift.
    /// </summary>
    /// <param name="data">The data content to project.</param>
    public static ChatMessagePart ToChatMessagePart(this DataContent data) =>
        data.MediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) && !data.Data.IsEmpty
            ? ChatMessagePart.FromText(Encoding.UTF8.GetString(data.Data.Span), data.MediaType, data.Name)
            : ChatMessagePart.FromText(data.Uri, data.MediaType, data.Name);

    /// <summary>
    /// Projects a <see cref="UriContent"/> — hosted content referenced by URL, typically an image — into a boundary
    /// <see cref="ChatMessagePart"/> carrying that absolute URL. Unlike <see cref="DataContent"/> the bytes never enter
    /// the stream or the message's JSON column, so this is the cheap way to attach media to a turn. Shared by the
    /// streaming projection (<c>ChatsService.StreamTurnAsync</c>) and the aggregated one (<see cref="ToDexChatMessage"/>).
    /// <para>
    /// The part carries no name: <see cref="UriContent"/> has no equivalent of <see cref="DataContent.Name"/>, so a
    /// hosted image that needs a caption has to travel in an <see cref="ImageReference"/> envelope instead.
    /// </para>
    /// </summary>
    /// <param name="uri">The URI content to project.</param>
    public static ChatMessagePart ToChatMessagePart(this UriContent uri) =>
        ChatMessagePart.FromText(uri.Uri.ToString(), uri.MediaType);

    /// <summary>Maps <see cref="UsageDetails"/> to the boundary <see cref="DexChatUsage"/>; <c>null</c> stays <c>null</c>. Question counters are the caller's to set.</summary>
    public static DexChatUsage? ToDexChatUsage(this UsageDetails? usage) => usage is null ? null : new DexChatUsage {
        InputTokenCount = usage.InputTokenCount,
        OutputTokenCount = usage.OutputTokenCount,
        TotalTokenCount = usage.TotalTokenCount,
        QuestionsUsedCount = usage.AdditionalCounts?.TryGetValue(nameof(DexChatUsage.QuestionsUsedCount), out var used) is true ? used : null,
        QuestionsLimitCount = usage.AdditionalCounts?.TryGetValue(nameof(DexChatUsage.QuestionsLimitCount), out var limit) is true ? limit : null
    };

    /// <summary>Maps a <see cref="ChatRole"/> to the boundary <see cref="DexChatRole"/>. Unknown roles map to <see cref="DexChatRole.User"/>.</summary>
    public static DexChatRole ToDexChatRole(this ChatRole role) => role.Value.ToLowerInvariant() switch {
        "assistant" => DexChatRole.Assistant,
        "system" => DexChatRole.System,
        "tool" => DexChatRole.Tool,
        "user" => DexChatRole.User,
        _ => DexChatRole.User
    };

    /// <summary>Maps a <see cref="ChatFinishReason"/> to the boundary <see cref="DexChatFinishReason"/>; <c>null</c> and unknown reasons map to <c>null</c>.</summary>
    public static DexChatFinishReason? ToDexChatFinishReason(this ChatFinishReason? finishReason) => finishReason?.Value?.ToLowerInvariant() switch {
        "stop" => DexChatFinishReason.Stop,
        "length" => DexChatFinishReason.Length,
        "tool_calls" => DexChatFinishReason.ToolCalls,
        "content_filter" => DexChatFinishReason.ContentFilter,
        "limit" => DexChatFinishReason.Limit,
        _ => null
    };


    /// <summary>Recovers the pipeline's <see cref="Citation"/> from an annotation — via <see cref="AIAnnotation.RawRepresentation"/> when present (in-process responses), else rebuilt best-effort from the annotation fields.</summary>
    private static Citation ToCitation(CitationAnnotation annotation) => annotation.RawRepresentation as Citation ?? new Citation {
        ChunkId = Guid.TryParse(annotation.FileId, out var chunkId) ? chunkId : Guid.Empty,
        Title = annotation.Title,
        Snippet = annotation.Snippet,
        SourceUrl = annotation.Url?.ToString()
    };
}
