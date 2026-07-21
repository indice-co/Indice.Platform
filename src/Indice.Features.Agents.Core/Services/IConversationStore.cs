using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.Extensions.AI;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Persistence boundary for chat <see cref="Conversation"/>s and their <see cref="ChatMessage"/>s.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Loads an existing conversation's metadata (no messages — history is loaded by the pipeline's chat-history
    /// provider) when <paramref name="conversationId"/> is supplied, or creates a new empty conversation when it is <c>null</c>.
    /// Returns <c>null</c> when <paramref name="conversationId"/> is supplied but no conversation matches <paramref name="userId"/>.
    /// </summary>
    Task<Conversation?> LoadOrCreateAsync(
        string userId,
        Guid? conversationId,
        CancellationToken cancellationToken);

    /// <summary>Returns the conversation detail (metadata + last <see cref="SessionOptions.HistoryWindow"/> turns of messages, oldest-first) or <c>null</c>.</summary>
    Task<Conversation?> GetAsync(Guid conversationId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the last <see cref="SessionOptions.HistoryWindow"/> turns of messages for <paramref name="conversationId"/>,
    /// oldest-first. Carries no ownership filter: callers sit behind the chats-service boundary, where the conversation
    /// has already been resolved against the requesting user.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, CancellationToken cancellationToken);

    /// <summary>Returns a paged list of conversations owned by <paramref name="userId"/>, most-recently-active first.</summary>
    Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a user/assistant turn against <paramref name="conversationId"/> in a single <c>SaveChanges</c>:
    /// inserts both messages, bumps <c>LastActivityAt</c>, increments cumulative token totals, and — when the
    /// conversation title is still <c>null</c> and the title-auto-generate option is enabled — derives a title from
    /// the user message. Returns the persisted assistant <see cref="ChatMessage"/>.
    /// </summary>
    Task<ChatMessage> AppendTurnAsync(Guid conversationId, ChatMessage userMessage, ChatResponse response, CancellationToken cancellationToken);

    /// <summary>Deletes a session and its messages. Returns affected row count (0 ⇒ not found / not owned).</summary>
    Task<int> DeleteAsync(Guid conversationId, string userId, CancellationToken cancellationToken);

    /// <summary>Counts the sessions owned by <paramref name="userId"/>.</summary>
    Task<int> CountSessionsAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Sums the reasoning-model tokens (prompt + completion) on the user's turns since <paramref name="since"/>.</summary>
    Task<long> GetUsageTokensAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken);
}
