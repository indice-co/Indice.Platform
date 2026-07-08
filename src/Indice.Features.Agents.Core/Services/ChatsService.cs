using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Types;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class ChatsService : IChatsService
{
    private readonly ISessionsStore _store;
    private readonly IDexRunner _runner;
    private readonly IUsageGuardService _usageGuard;
    private readonly AgentsOptions.AzureOpenAIDeployments _deployments;

    /// <summary>Creates a new <see cref="ChatsService"/>.</summary>
    public ChatsService(ISessionsStore store, IDexRunner runner, IUsageGuardService usageGuard, IOptions<AgentsOptions> options) {
        _store = store;
        _runner = runner;
        _usageGuard = usageGuard;
        _deployments = options.Value.AzureOpenAI.Deployments;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse?> SendAsync(string userId, Guid? sessionId, string text, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, sessionId, cancellationToken);
        var session = await _store.LoadOrCreateAsync(userId, sessionId, cancellationToken);
        if (session is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(session);
        if (!turnCheck.Allowed) {
            return new ChatResponse {
                SessionId = session.Id,
                MessageId = Guid.Empty,
                Answer = turnCheck.Message,
                LimitReached = true,
            };
        }
        var userNow = DateTimeOffset.UtcNow;
        var result = await _runner.RunAsync( new RagRequest { Question = text, SessionId = session.Id }, cancellationToken);
        var assistantNow = DateTimeOffset.UtcNow;
        var userMessage = new ChatMessage {
            Id = Guid.NewGuid(),
            Role = ChatMessageRole.User,
            Content = text,
            CreatedAt = userNow,
        };
        var assistantText = result.Answer ?? string.Empty;
        var assistantMessage = new ChatMessage {
            Id = Guid.NewGuid(),
            Role = ChatMessageRole.Assistant,
            Content = assistantText,
            CreatedAt = assistantNow,
        };

        var persistedAssistant = await _store.AppendTurnAsync(session.Id, userMessage, assistantMessage,
            promptTokens: result.Usage?.InputTokenCount ?? 0, completionTokens: result.Usage?.OutputTokenCount ?? 0,
            modelUsed: result.ModelUsed ?? _deployments.Reasoning, cancellationToken);

        return new ChatResponse {
            SessionId = session.Id,
            MessageId = persistedAssistant.Id,
            Answer = assistantText,
            Citations = result.Citations,
            Failed = result.Failed,
            FailureReason = result.FailureReason,
        };
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<SseItem<ChatStreamEvent>>?> SendStreamAsync(string userId, Guid? sessionId, string text, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, sessionId, cancellationToken);
        var session = await _store.LoadOrCreateAsync(userId, sessionId, cancellationToken);
        if (session is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(session);
        if (!turnCheck.Allowed) {
            return LimitReachedStream(session.Id, turnCheck.Message);
        }
        return StreamTurnAsync(session, text, cancellationToken);
    }

    /// <summary>Throws a <see cref="BusinessException"/> when a new session is requested but the user's session cap is hit. No-op for existing sessions.</summary>
    private async Task EnsureSessionCreationAllowedAsync(string userId, Guid? sessionId, CancellationToken cancellationToken) {
        if (sessionId is not null) {
            return;
        }
        var creationCheck = await _usageGuard.CheckSessionCreationAsync(userId, cancellationToken);
        if (!creationCheck.Allowed) {
            throw new BusinessException(creationCheck.Message, "SESSIONS_LIMIT_REACHED");
        }
    }

    /// <summary>Single-event stream carrying the guard's predefined reply for a blocked turn. Nothing is persisted.</summary>
    private static async IAsyncEnumerable<SseItem<ChatStreamEvent>> LimitReachedStream(Guid sessionId, string? message) {
        yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent {
            Type = "complete",
            SessionId = sessionId,
            MessageId = Guid.Empty,
            Answer = message,
            Citations = Array.Empty<Citation>(),
            LimitReached = true,
        }, eventType: "complete");
        await Task.CompletedTask;
    }

    /// <summary>Maps the runner's stream to SSE items, then persists the turn and emits the terminal <c>complete</c> event.</summary>
    private async IAsyncEnumerable<SseItem<ChatStreamEvent>> StreamTurnAsync(
        Session session, string text, [EnumeratorCancellation] CancellationToken cancellationToken) {
        var request = new RagRequest { Question = text, SessionId = session.Id };
        DexFinalEvent? final = null;
        await foreach (var evt in _runner.RunStreamingAsync(request, cancellationToken)) {
            switch (evt) {
                case DexStepEvent step:
                    yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "step", Step = step.Label }, eventType: "step");
                    break;
                case DexDeltaEvent delta:
                    yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "delta", Text = delta.Text }, eventType: "delta");
                    break;
                case DexFinalEvent f:
                    final = f;
                    break;
            }
        }
        var complete = await PersistTurnAsync(session, text, final, cancellationToken);
        yield return new SseItem<ChatStreamEvent>(complete, eventType: "complete");
    }

    /// <summary>Persists the user/assistant turn (mirroring <see cref="SendAsync"/>) and builds the terminal <c>complete</c> event.</summary>
    private async Task<ChatStreamEvent> PersistTurnAsync(Session session, string text, DexFinalEvent? final, CancellationToken cancellationToken) {
        var userNow = DateTimeOffset.UtcNow;
        var assistantText = final?.Answer ?? string.Empty;
        var assistantNow = DateTimeOffset.UtcNow;
        var userMessage = new ChatMessage { Id = Guid.NewGuid(), Role = ChatMessageRole.User, Content = text, CreatedAt = userNow };
        var assistantMessage = new ChatMessage { Id = Guid.NewGuid(), Role = ChatMessageRole.Assistant, Content = assistantText, CreatedAt = assistantNow };

        var persistedAssistant = await _store.AppendTurnAsync(session.Id, userMessage, assistantMessage,
            promptTokens: final?.Usage?.InputTokenCount ?? 0, completionTokens: final?.Usage?.OutputTokenCount ?? 0,
            modelUsed: final?.ModelUsed ?? _deployments.Reasoning, cancellationToken);

        return new ChatStreamEvent {
            Type = "complete",
            SessionId = session.Id,
            MessageId = persistedAssistant.Id,
            Answer = assistantText,
            Citations = final?.Citations ?? Array.Empty<Citation>(),
            Failed = final?.Failed ?? false,
            FailureReason = final?.FailureReason,
        };
    }

    /// <inheritdoc/>
    public Task<Session?> GetAsync(string userId, Guid sessionId, CancellationToken cancellationToken)
        => _store.GetAsync(sessionId, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<ResultSet<SessionListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken)
        => _store.ListAsync(userId, options, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string userId, Guid sessionId, CancellationToken cancellationToken) {
        var deleted = await _store.DeleteAsync(sessionId, userId, cancellationToken);
        return deleted > 0;
    }
}
