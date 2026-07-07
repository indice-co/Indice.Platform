using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Types;
using Microsoft.Extensions.Options;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AIChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class ChatsService : IChatsService
{
    private readonly ISessionsStore _store;
    private readonly IDexRunner _runner;
    private readonly AgentsOptions.AzureOpenAIDeployments _deployments;

    /// <summary>Creates a new <see cref="ChatsService"/>.</summary>
    public ChatsService(ISessionsStore store, IDexRunner runner, IOptions<AgentsOptions> options) {
        _store = store;
        _runner = runner;
        _deployments = options.Value.AzureOpenAI.Deployments;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse?> SendAsync(string userId, Guid? sessionId, string text, CancellationToken cancellationToken) {
        var session = await _store.LoadOrCreateAsync(userId, sessionId, cancellationToken);
        if (session is null) {
            return null;
        }
        var userNow = DateTimeOffset.UtcNow;
        var result = await _runner.RunAsync(
            new RagRequest { Question = text, History = ToAIMessages(session.Messages) },
            cancellationToken);
        var assistantNow = DateTimeOffset.UtcNow;
        var userMessage = new ChatMessage {
            Id = Guid.NewGuid(),
            Role = ChatMessageRole.User,
            Content = text,
            CreatedAt = userNow,
        };
        // Out-of-scope refusal text comes through on Answer (from OutOfScopeResponder); Failed signals a step failure, surfaced via FailureReason on the response.
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
        var session = await _store.LoadOrCreateAsync(userId, sessionId, cancellationToken);
        if (session is null) {
            return null;
        }
        return StreamTurnAsync(session, text, cancellationToken);
    }

    /// <summary>Maps the runner's stream to SSE items, then persists the turn and emits the terminal <c>complete</c> event.</summary>
    private async IAsyncEnumerable<SseItem<ChatStreamEvent>> StreamTurnAsync(
        Session session, string text, [EnumeratorCancellation] CancellationToken cancellationToken) {
        var request = new RagRequest { Question = text, History = ToAIMessages(session.Messages) };
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
        // The runner yields exactly one terminal DexFinalEvent on success or failure; a mid-stream cancellation
        // throws out of the foreach above (the run just stops), so we only reach here on a completed run.
        var complete = await PersistTurnAsync(session, text, final, cancellationToken);
        yield return new SseItem<ChatStreamEvent>(complete, eventType: "complete");
    }

    /// <summary>Persists the user/assistant turn (mirroring <see cref="SendAsync"/>) and builds the terminal <c>complete</c> event.</summary>
    private async Task<ChatStreamEvent> PersistTurnAsync(Session session, string text, DexFinalEvent? final, CancellationToken cancellationToken) {
        var now = DateTimeOffset.UtcNow;
        var assistantText = final?.Answer ?? string.Empty;
        var userMessage = new ChatMessage { Id = Guid.NewGuid(), Role = ChatMessageRole.User, Content = text, CreatedAt = now };
        var assistantMessage = new ChatMessage { Id = Guid.NewGuid(), Role = ChatMessageRole.Assistant, Content = assistantText, CreatedAt = now };

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

    /// <summary>Projects the persisted session turns into the framework message shape the pipeline consumes.</summary>
    private static IReadOnlyList<AIChatMessage> ToAIMessages(IReadOnlyList<ChatMessage> messages)
        => messages.Select(m => new AIChatMessage(ToAIChatRole(m.Role), m.Content)).ToList();

    /// <summary>Maps the persisted <see cref="ChatMessageRole"/> onto the framework role the MAF pipeline consumes.</summary>
    private static AIChatRole ToAIChatRole(ChatMessageRole role) => role switch {
        ChatMessageRole.User => AIChatRole.User,
        ChatMessageRole.Assistant => AIChatRole.Assistant,
        ChatMessageRole.System => AIChatRole.System,
        ChatMessageRole.Tool => AIChatRole.Tool,
        _ => AIChatRole.User,
    };

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
