using System.Net.ServerSentEvents;
using Indice.Features.Agents.Core.Models;
using Indice.Types;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Orchestrates conversation lifecycle: posting turns through the RAG pipeline and exposing the persisted history.</summary>
public interface IChatsService
{
    /// <summary>
    /// Posts a turn. When <paramref name="conversationId"/> is <c>null</c>, the conversation is created inline as part of this call.
    /// Returns <c>null</c> when <paramref name="conversationId"/> is supplied but no conversation matches <paramref name="userId"/>.
    /// </summary>
    Task<DexChatResponse?> SendAsync(string userId, Guid? conversationId, ChatRequest chatRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Streaming counterpart of <see cref="SendAsync"/>: posts a turn and returns the live SSE event stream
    /// (pipeline-step progress, answer deltas, then a terminal <c>complete</c> event). The turn is persisted
    /// once the stream completes. Returns <c>null</c> when <paramref name="conversationId"/> is supplied but no
    /// session matches <paramref name="userId"/> — letting the caller respond 404 before any event is sent.
    /// </summary>
    Task<IAsyncEnumerable<SseItem<DexChatResponseUpdate>>?> SendStreamAsync(string userId, Guid? conversationId, string text, CancellationToken cancellationToken);

    /// <summary>Returns a conversation detail (metadata + recent messages) or <c>null</c> when not found / not owned.</summary>
    Task<DexConversation?> GetAsync(string userId, Guid conversationId, CancellationToken cancellationToken);

    /// <summary>Returns a paged listing of the caller's conversations, most-recently-active first.</summary>
    Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken);

    /// <summary>Deletes a conversation and its messages. Returns <c>true</c> when the conversation existed and was deleted.</summary>
    Task<bool> DeleteAsync(string userId, Guid conversationId, CancellationToken cancellationToken);

    /// <summary>Likes or dislikes a conversation message.</summary>
    /// <returns><c>true</c> when the message exists and the like/dislike was persisted.</returns>
    Task<bool> SetLikeAsync(string userId, Guid conversationId, Guid messageId, bool? liked, CancellationToken cancellationToken);
}
