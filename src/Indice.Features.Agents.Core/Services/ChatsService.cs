using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class ChatsService : IChatsService
{
    private readonly ISessionsStore _store;
    private readonly IDexChatClient _dexClient;
    private readonly IUsageGuardService _usageGuard;
    private readonly AgentsOptions.AzureOpenAIDeployments _deployments;
    private readonly AgentsOptions.SessionOptions _sessionOptions;

    /// <summary>Creates a new <see cref="ChatsService"/>.</summary>
    public ChatsService(ISessionsStore store, IDexChatClient dexClient, IUsageGuardService usageGuard, IOptions<AgentsOptions> options) {
        _store = store;
        _dexClient = dexClient;
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
        var userMessage = new ChatMessage(ChatRole.User, chatRequest.Text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var result = await _dexClient.GetResponseAsync(userMessage, new ChatOptions { ConversationId = session.Id.ToString() }, cancellationToken);
        
        var persistedAssistant = await _store.AppendTurnAsync(session.Id, userMessage, result.Messages.First(),
            promptTokens: result.Usage?.InputTokenCount ?? 0, completionTokens: result.Usage?.OutputTokenCount ?? 0,
            modelUsed: result.ModelId ?? _deployments.Reasoning, cancellationToken);
        result.Usage ??= new UsageDetails();
        result.Usage.AdditionalCounts = new() {
            ["questionsUsed"] = _sessionOptions.GetQuestionsUsed(session.MessageCount) ?? 0,
            ["questionsTotal"] = _sessionOptions.GetQuestionsTotal() ?? 0
        };
        return result;
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


        var userMessage = new ChatMessage(ChatRole.User, text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        ChatResponseUpdate? final = null;
        UsageContent? usageContent = null;
        string? failure = null;
        await foreach (var evt in _dexClient.GetStreamingResponseAsync(userMessage, new ChatOptions { ConversationId = session.Id.ToString() }, cancellationToken)) {
            switch (evt.RawRepresentation) {
                case "Step":
                    yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "step", Step = evt.Text }, eventType: "step");
                    break;
                case "Delta":
                    yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "delta", Text = evt.Text }, eventType: "delta");
                    break;
                case "Usage":
                    usageContent = evt.Contents.FirstOrDefault() as UsageContent;
                    break;
                case "Failure":
                    failure = evt.Text;
                    break;
                case "Final":
                    final = evt;
                    break;
            }
        }

        var assistantMessage = new ChatMessage(ChatRole.Assistant, final?.Text ?? "") {
            MessageId = final?.MessageId ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var persistedAssistant = await _store.AppendTurnAsync(session.Id, userMessage, assistantMessage,
            promptTokens: usageContent?.Details?.InputTokenCount ?? 0, completionTokens: usageContent?.Details?.OutputTokenCount ?? 0,
            modelUsed: final?.ModelId ?? _deployments.Reasoning, cancellationToken);

        var finalEvent = new ChatStreamEvent {
            Type = "complete",
            SessionId = session.Id,
            MessageId = persistedAssistant.MessageId,
            Answer = assistantMessage.Text,
            Citations = final?.AdditionalProperties?["citations"] as IReadOnlyList<Citation> ?? [],
            Sources = final?.AdditionalProperties?["sources"] as IReadOnlyList<SourceDocumentLink> ?? [],
            Failed = failure != null,
            FailureReason = failure,
            QuestionsUsed = _sessionOptions.GetQuestionsUsed(session.MessageCount + 2),
            QuestionsTotal = _sessionOptions.GetQuestionsTotal(),
        };
        yield return new SseItem<ChatStreamEvent>(finalEvent, eventType: "complete");
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
