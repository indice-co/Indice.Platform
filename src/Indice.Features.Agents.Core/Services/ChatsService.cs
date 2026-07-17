using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class ChatsService : IChatsService
{
    private readonly ISessionsStore _store;
    private readonly IDexRunner _runner;
    private readonly IUsageGuardService _usageGuard;
    private readonly AgentsOptions.AzureOpenAIDeployments _deployments;
    private readonly AgentsOptions.SessionOptions _sessionOptions;

    /// <summary>Creates a new <see cref="ChatsService"/>.</summary>
    public ChatsService(ISessionsStore store, IDexRunner runner, IUsageGuardService usageGuard, IOptions<AgentsOptions> options) {
        _store = store;
        _runner = runner;
        _usageGuard = usageGuard;
        _deployments = options.Value.AzureOpenAI.Deployments;
        _sessionOptions = options.Value.Session;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse?> SendAsync(string userId, Guid? sessionId, ChatRequest chatRequest, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, sessionId, cancellationToken);
        var session = await _store.LoadOrCreateAsync(userId, sessionId, cancellationToken);
        if (session is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(session);
        if (!turnCheck.Allowed) {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, turnCheck.Message)) {
                ConversationId = session.Id.ToString(),
                ResponseId = Guid.NewGuid().ToString(),
                Usage = new () {
                    AdditionalCounts = new() {
                        ["questionsUsed"] = _sessionOptions.GetQuestionsUsed(session.MessageCount) ?? 0,
                        ["questionsTotal"] = _sessionOptions.GetQuestionsTotal() ?? 0
                    }
                },
                AdditionalProperties = new() {
                    ["limitReached"] = true
                }
            };
        }
        var request = new RagRequest { Question = chatRequest.Text, SessionId = session.Id };
        var result = await _runner.RunAsync(request, cancellationToken);
        var userMessage = new ChatMessage(ChatRole.User, request.Question) { 
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = request.TimeStamp
        };
        var assistantText = new TextContent(result.Answer ?? string.Empty) {
            Annotations = result.Citations?.Select(c => (AIAnnotation)new CitationAnnotation {
                FileId = c.DocumentId.ToString(),
                Title = result.Sources.First(x => x.Id == c.DocumentId).SourceTitle,
                Url = new Uri(result.Sources.First(x => x.Id == c.DocumentId).SourceUrl),
                Snippet = c.Title
                // Other metadata like page number, confidence, etc.
            }).ToList() ?? []
        };
        var assistantMessage = new ChatMessage(ChatRole.Assistant, [assistantText]) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var persistedAssistant = await _store.AppendTurnAsync(session.Id, userMessage, assistantMessage,
            promptTokens: result.Usage?.InputTokenCount ?? 0, completionTokens: result.Usage?.OutputTokenCount ?? 0,
            modelUsed: result.ModelUsed ?? _deployments.Reasoning, cancellationToken);

        return new ChatResponse {
            ConversationId = session.Id.ToString(),
            ResponseId = persistedAssistant.MessageId,
            Messages = [assistantMessage],
            ModelId = result.ModelUsed ?? _deployments.Reasoning,
            Usage = new() {
                InputTokenCount = result.Usage?.InputTokenCount ?? 0,
                OutputTokenCount = result.Usage?.OutputTokenCount ?? 0,
                AdditionalCounts = new() {
                    ["questionsUsed"] = _sessionOptions.GetQuestionsUsed(session.MessageCount) ?? 0,
                    ["questionsTotal"] = _sessionOptions.GetQuestionsTotal() ?? 0
                }
            },
            AdditionalProperties = new() {
                ["failed"] = result.Failed,
                ["failureReason"] = result.FailureReason,
                ["sources"] = result.Sources ?? [],
            }
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
            return LimitReachedStream(session, turnCheck.Message);
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
    private async IAsyncEnumerable<SseItem<ChatStreamEvent>> LimitReachedStream(Session session, string? message) {
        yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent {
            Type = "complete",
            SessionId = session.Id,
            MessageId = Guid.Empty.ToString(),
            Answer = message,
            Citations = [],
            Sources = [],
            LimitReached = true,
            QuestionsUsed = _sessionOptions.GetQuestionsUsed(session.MessageCount),
            QuestionsTotal = _sessionOptions.GetQuestionsTotal(),
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
        // The runner yields exactly one terminal DexFinalEvent on success or failure; a mid-stream cancellation
        // throws out of the foreach above (the run just stops), so we only reach here on a completed run.
        var complete = await PersistTurnAsync(session, request, final, cancellationToken);
        yield return new SseItem<ChatStreamEvent>(complete, eventType: "complete");
    }

    /// <summary>Persists the user/assistant turn (mirroring <see cref="SendAsync"/>) and builds the terminal <c>complete</c> event.</summary>
    private async Task<ChatStreamEvent> PersistTurnAsync(Session session, RagRequest request, DexFinalEvent? final, CancellationToken cancellationToken) {
        var userMessage = new ChatMessage(ChatRole.User, request.Question) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = request.TimeStamp
        };

        var assistantText = new TextContent(final?.Answer ?? string.Empty) {
            Annotations = final?.Citations?.Select(c => (AIAnnotation)new CitationAnnotation {
                FileId = c.DocumentId.ToString(),
                Title = final?.Sources.First(x => x.Id == c.DocumentId).SourceTitle,
                Url = new Uri(final!.Sources.First(x => x.Id == c.DocumentId).SourceUrl),
                Snippet = c.Title
                // Other metadata like page number, confidence, etc.
            }).ToList() ?? []
        };
        var assistantMessage = new ChatMessage(ChatRole.Assistant, [assistantText]) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var persistedAssistant = await _store.AppendTurnAsync(session.Id, userMessage, assistantMessage,
            promptTokens: final?.Usage?.InputTokenCount ?? 0, completionTokens: final?.Usage?.OutputTokenCount ?? 0,
            modelUsed: final?.ModelUsed ?? _deployments.Reasoning, cancellationToken);

        return new ChatStreamEvent {
            Type = "complete",
            SessionId = session.Id,
            MessageId = persistedAssistant.MessageId,
            Answer = assistantText.Text,
            Citations = final?.Citations ?? [],
            Sources = final?.Sources ?? [],
            Failed = final?.Failed ?? false,
            FailureReason = final?.FailureReason,
            QuestionsUsed = _sessionOptions.GetQuestionsUsed(session.MessageCount + 2),
            QuestionsTotal = _sessionOptions.GetQuestionsTotal(),
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
