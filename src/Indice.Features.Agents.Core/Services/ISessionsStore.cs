using Indice.Features.Agents.Core.Models;
using Indice.Types;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Persistence boundary for chat <see cref="Session"/>s and their <see cref="ChatMessage"/>s.
/// </summary>
public interface ISessionsStore
{
    /// <summary>
    /// Loads an existing session (with the last <see cref="SessionOptions.HistoryWindow"/> turns of messages,
    /// oldest-first) when <paramref name="sessionId"/> is supplied, or creates a new empty session when it is <c>null</c>.
    /// Returns <c>null</c> when <paramref name="sessionId"/> is supplied but no session matches <paramref name="userId"/>.
    /// </summary>
    Task<Session?> LoadOrCreateAsync(
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken);

    /// <summary>Returns the session detail (metadata + last <see cref="SessionOptions.HistoryWindow"/> turns of messages, oldest-first) or <c>null</c>.</summary>
    Task<Session?> GetAsync(Guid sessionId, string userId, CancellationToken cancellationToken);

    /// <summary>Returns a paged list of sessions owned by <paramref name="userId"/>, most-recently-active first.</summary>
    Task<ResultSet<SessionListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a user/assistant turn against <paramref name="sessionId"/> in a single <c>SaveChanges</c>:
    /// inserts both messages, bumps <c>LastActivityAt</c>, increments cumulative token totals, and — when the
    /// session title is still <c>null</c> and the title-auto-generate option is enabled — derives a title from
    /// the user message. Returns the persisted assistant <see cref="ChatMessage"/>.
    /// </summary>
    Task<ChatMessage> AppendTurnAsync(
        Guid sessionId,
        ChatMessage userMessage,
        ChatMessage assistantMessage,
        long promptTokens,
        long completionTokens,
        string? modelUsed,
        CancellationToken cancellationToken);

    /// <summary>Deletes a session and its messages. Returns affected row count (0 ⇒ not found / not owned).</summary>
    Task<int> DeleteAsync(Guid sessionId, string userId, CancellationToken cancellationToken);

    /// <summary>Sums the reasoning-model tokens (prompt + completion) on the user's turns since <paramref name="since"/>.</summary>
    Task<long> GetUsageTokensAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken);
}
