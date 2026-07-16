using System.Net.ServerSentEvents;
using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Orchestrates chat session lifecycle: posting turns through the RAG pipeline and exposing the persisted history.</summary>
public interface IChatsService
{
    /// <summary>
    /// Posts a turn. When <paramref name="sessionId"/> is <c>null</c>, the session is created inline as part of this call.
    /// Returns <c>null</c> when <paramref name="sessionId"/> is supplied but no session matches <paramref name="userId"/>.
    /// </summary>
    Task<ChatResponse?> SendAsync(string userId, Guid? sessionId, ChatRequest chatRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Streaming counterpart of <see cref="SendAsync"/>: posts a turn and returns the live SSE event stream
    /// (pipeline-step progress, answer deltas, then a terminal <c>complete</c> event). The turn is persisted
    /// once the stream completes. Returns <c>null</c> when <paramref name="sessionId"/> is supplied but no
    /// session matches <paramref name="userId"/> — letting the caller respond 404 before any event is sent.
    /// </summary>
    Task<IAsyncEnumerable<SseItem<ChatStreamEvent>>?> SendStreamAsync(string userId, Guid? sessionId, string text, CancellationToken cancellationToken);

    /// <summary>Returns a session detail (metadata + recent messages) or <c>null</c> when not found / not owned.</summary>
    Task<Session?> GetAsync(string userId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>Returns a paged listing of the caller's sessions, most-recently-active first.</summary>
    Task<ResultSet<SessionListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken);

    /// <summary>Deletes a session and its messages. Returns <c>true</c> when the session existed and was deleted.</summary>
    Task<bool> DeleteAsync(string userId, Guid sessionId, CancellationToken cancellationToken);
}
