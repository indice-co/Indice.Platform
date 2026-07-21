using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class ChatsService : IChatsService
{
    private readonly IConversationStore _store;
    private readonly IDexChatClient _dexClient;
    private readonly IUsageGuardService _usageGuard;
    private readonly AgentsOptions.AzureOpenAIDeployments _deployments;
    private readonly AgentsOptions.SessionOptions _sessionOptions;

    /// <summary>Creates a new <see cref="ChatsService"/>.</summary>
    public ChatsService(IConversationStore store, IDexChatClient dexClient, IUsageGuardService usageGuard, IOptions<AgentsOptions> options) {
        _store = store;
        _dexClient = dexClient;
        _usageGuard = usageGuard;
        _deployments = options.Value.AzureOpenAI.Deployments;
        _sessionOptions = options.Value.Session;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse?> SendAsync(string userId, Guid? conversationId, ChatRequest chatRequest, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, conversationId, cancellationToken);
        var conversation = await _store.LoadOrCreateAsync(userId, conversationId, cancellationToken);
        if (conversation is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(conversation);
        if (!turnCheck.Allowed) {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, turnCheck.Message)) {
                ConversationId = conversation.Id.ToString(),
                ResponseId = Guid.NewGuid().ToString(),
                Usage = new() {
                    AdditionalCounts = new() {
                        ["questionsUsed"] = _sessionOptions.GetQuestionsUsed(conversation.MessageCount) ?? 0,
                        ["questionsTotal"] = _sessionOptions.GetQuestionsTotal() ?? 0
                    }
                },
                FinishReason = new ChatFinishReason("Limit")
            };
        }
        var userMessage = new ChatMessage(ChatRole.User, chatRequest.Text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var result = await _dexClient.GetResponseAsync(userMessage, new ChatOptions { ConversationId = conversation.Id.ToString() }, cancellationToken);
        var persistedAssistant = await _store.AppendTurnAsync(conversation.Id, userMessage, result.Messages.First(),
            promptTokens: result.Usage?.InputTokenCount ?? 0, completionTokens: result.Usage?.OutputTokenCount ?? 0,
            modelUsed: string.IsNullOrWhiteSpace(result.ModelId) ? _deployments.Reasoning : result.ModelId, cancellationToken);
        result.Usage ??= new UsageDetails();
        result.Usage.AdditionalCounts = new() {
            ["questionsUsed"] = _sessionOptions.GetQuestionsUsed(conversation.MessageCount) ?? 0,
            ["questionsTotal"] = _sessionOptions.GetQuestionsTotal() ?? 0
        };
        return result;
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<SseItem<ChatStreamEvent>>?> SendStreamAsync(string userId, Guid? conversationId, string text, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, conversationId, cancellationToken);
        var session = await _store.LoadOrCreateAsync(userId, conversationId, cancellationToken);
        if (session is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(session);
        if (!turnCheck.Allowed) {
            return LimitReachedStream(session, turnCheck.Message);
        }
        return StreamTurnAsync(session, text, cancellationToken);
    }

    /// <summary>Throws a <see cref="BusinessException"/> when a new conversation is requested but the user's conversation cap is hit. No-op for existing sessions.</summary>
    private async Task EnsureSessionCreationAllowedAsync(string userId, Guid? sessionId, CancellationToken cancellationToken) {
        if (sessionId is not null) {
            return;
        }
        var creationCheck = await _usageGuard.CheckConversationCreationAsync(userId, cancellationToken);
        if (!creationCheck.Allowed) {
            throw new BusinessException(creationCheck.Message, "SESSIONS_LIMIT_REACHED");
        }
    }

    /// <summary>Single-event stream carrying the guard's predefined reply for a blocked turn. Nothing is persisted.</summary>
    private async IAsyncEnumerable<SseItem<ChatStreamEvent>> LimitReachedStream(Conversation conversation, string? message) {
        yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent {
            Type = "complete",
            ConversationId = conversation.Id,
            MessageId = Guid.Empty.ToString(),
            Answer = message,
            Citations = [],
            Sources = [],
            LimitReached = true,
            QuestionsUsed = _sessionOptions.GetQuestionsUsed(conversation.MessageCount),
            QuestionsTotal = _sessionOptions.GetQuestionsTotal(),
        }, eventType: "complete");
        await Task.CompletedTask;
    }

    /// <summary>Maps the runner's stream to SSE items, then persists the turn and emits the terminal <c>complete</c> event.</summary>
    private async IAsyncEnumerable<SseItem<ChatStreamEvent>> StreamTurnAsync(
        Conversation session, string text, [EnumeratorCancellation] CancellationToken cancellationToken) {


        var userMessage = new ChatMessage(ChatRole.User, text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        ChatResponseUpdate? final = null;
        UsageContent? usageContent = null;
        string? failure = null;
        await foreach (var evt in _dexClient.GetStreamingResponseAsync(userMessage, new ChatOptions { ConversationId = session.Id.ToString() }, cancellationToken)) {
            // Progress/"thinking" updates carry TextReasoningContent; they are never part of the answer text.
            if (evt.Contents.OfType<TextReasoningContent>().FirstOrDefault() is { } reasoning) {
                yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "step", Step = reasoning.Text }, eventType: "step");
                continue;
            }
            if (evt.Contents.OfType<UsageContent>().FirstOrDefault() is { } usage) {
                usageContent = usage;
                continue;
            }
            if (evt.Contents.OfType<ErrorContent>().FirstOrDefault() is { } error) {
                failure = error.Message;
                continue;
            }
            if (evt.FinishReason is not null) {
                final = evt;
                continue;
            }
            if (!string.IsNullOrEmpty(evt.Text)) {
                yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "delta", Text = evt.Text }, eventType: "delta");
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
            ConversationId = session.Id,
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
    public Task<Conversation?> GetAsync(string userId, Guid conversationId, CancellationToken cancellationToken)
        => _store.GetAsync(conversationId, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken)
        => _store.ListAsync(userId, options, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string userId, Guid conversationId, CancellationToken cancellationToken) {
        var deleted = await _store.DeleteAsync(conversationId, userId, cancellationToken);
        return deleted > 0;
    }
}
