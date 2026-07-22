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
    /// Maps a <see cref="ChatMessage"/> to the boundary <see cref="DexChatMessage"/>: non-empty textual contents flatten
    /// to <see cref="ChatMessagePart"/>s and <see cref="CitationAnnotation"/>s lift to <see cref="Citation"/>s
    /// (deduplicated per cited chunk — annotations carry one entry per marker occurrence).
    /// </summary>
    public static DexChatMessage ToDexChatMessage(this ChatMessage message) {
        var content = new ChatMessageContent();
        var citations = new List<Citation>();
        foreach (var item in message.Contents) {
            switch(item) {
                case TextContent text when !string.IsNullOrEmpty(text.Text):
                    content.AddPart(text.Text, "text/markdown");
                    citations.AddRange((text.Annotations ?? []).OfType<CitationAnnotation>().Select(ToCitation));
                    break;
                case DataContent data:
                    content.AddPart(data.Uri, data.MediaType);
                    break;
            }
        }
        return new DexChatMessage {
            MessageId = message.MessageId,
            AuthorName = message.AuthorName,
            Role = message.Role.ToDexChatRole(),
            Content = content,
            CreatedAt = message.CreatedAt,
            Citations = citations.DistinctBy(citation => citation.ChunkId).OrderBy(citation => citation.Number).ToList()
        };
    }

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
        _ => DexChatRole.System
    };

    /// <summary>Maps a <see cref="ChatFinishReason"/> to the boundary <see cref="DexChatFinishReason"/>; <c>null</c> and unknown reasons map to <c>null</c>.</summary>
    public static DexChatFinishReason? ToDexChatFinishReason(this ChatFinishReason? finishReason) => finishReason?.Value.ToLowerInvariant() switch {
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
