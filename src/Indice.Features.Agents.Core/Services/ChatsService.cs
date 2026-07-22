using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

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
    public async Task<DexChatResponse?> SendAsync(string userId, Guid? conversationId, ChatRequest chatRequest, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, conversationId, cancellationToken);
        var conversation = await _store.LoadOrCreateAsync(userId, conversationId, cancellationToken);
        if (conversation is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(conversation);
        if (!turnCheck.Allowed) {
            return new DexChatResponse {
                ConversationId = conversation.Id,
                ResponseId = Guid.NewGuid().ToString(),
                Messages = [new DexChatMessage {
                    MessageId = Guid.Empty.ToString(),
                    Role = DexChatRole.Assistant,
                    Content = new ChatMessageContent(turnCheck.Message ?? string.Empty),
                    CreatedAt = DateTimeOffset.UtcNow
                }],
                FinishReason = DexChatFinishReason.Limit
            };
        }
        var userMessage = new ChatMessage(ChatRole.User, chatRequest.Text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var response = await _dexClient.GetResponseAsync(userMessage, new ChatOptions { ConversationId = conversation.Id.ToString() }, cancellationToken);
        await _store.AppendTurnAsync(conversation.Id, userMessage, response, cancellationToken);

        var dexResponse = response.ToDexChatResponse();
        dexResponse.ConversationId ??= conversation.Id;
        dexResponse.Usage ??= new DexChatUsage();
        dexResponse.Usage.QuestionsUsedCount = _sessionOptions.GetQuestionsUsed(conversation.MessageCount);
        dexResponse.Usage.QuestionsLimitCount = _sessionOptions.GetQuestionsTotal();
        return dexResponse;
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<SseItem<ChatStreamEvent>>?> SendStreamAsync(string userId, Guid? conversationId, string text, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, conversationId, cancellationToken);
        var conversation = await _store.LoadOrCreateAsync(userId, conversationId, cancellationToken);
        if (conversation is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(conversation);
        if (!turnCheck.Allowed) {
            return LimitReachedStream(conversation, turnCheck.Message);
        }
        return StreamTurnAsync(conversation, text, cancellationToken);
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
        Conversation conversation, string text, [EnumeratorCancellation] CancellationToken cancellationToken) {

        var userMessage = new ChatMessage(ChatRole.User, text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var stream = _dexClient.GetStreamingResponseAsync(userMessage, new ChatOptions { ConversationId = conversation.Id.ToString() }, cancellationToken);
        var updates = new List<ChatResponseUpdate>();
        await foreach (var evt in stream) {
            if (!evt.Contents.Any(x => x is StepProgressContent)) {
                updates.Add(evt);
            }
            // Progress/"thinking" updates carry TextReasoningContent; they are never part of the answer text.
            if (evt.Contents.OfType<StepProgressContent>().FirstOrDefault() is { } stepUpdate) {
                yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "step", Step = stepUpdate.Label }, eventType: "step");
                continue;
            }
            if (!string.IsNullOrEmpty(evt.Text)) {
                yield return new SseItem<ChatStreamEvent>(new ChatStreamEvent { Type = "delta", Text = evt.Text }, eventType: "delta");
            }
        }

        var response = updates.ToChatResponse();

        await _store.AppendTurnAsync(conversation.Id, userMessage, response, cancellationToken);

        var finalEvent = new ChatStreamEvent {
            Type = "complete",
            ConversationId = conversation.Id,
            MessageId = response.Messages.First().MessageId,
            Answer = response.Text,
            //Citations = final?.AdditionalProperties?["citations"] as IReadOnlyList<Citation> ?? [],
            //Sources = final?.AdditionalProperties?["sources"] as IReadOnlyList<SourceDocumentLink> ?? [],
            //Failed = failure != null,
            //FailureReason = failure,
            QuestionsUsed = _sessionOptions.GetQuestionsUsed(conversation.MessageCount + 2),
            QuestionsTotal = _sessionOptions.GetQuestionsTotal(),
        };
        yield return new SseItem<ChatStreamEvent>(finalEvent, eventType: "complete");
    }


    /// <inheritdoc/>
    public async Task<DexConversation?> GetAsync(string userId, Guid conversationId, CancellationToken cancellationToken) {
        var conversation = await _store.GetAsync(conversationId, userId, cancellationToken);
        if (conversation is null) {
            return null;
        }
        return new DexConversation {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            LastActivityAt = conversation.LastActivityAt,
            MessageCount = conversation.MessageCount,
            Usage = new DexChatUsage {
                InputTokenCount = conversation.InputTokenCount,
                OutputTokenCount = conversation.OutputTokenCount,
                TotalTokenCount = conversation.InputTokenCount + conversation.OutputTokenCount,
                QuestionsUsedCount = _sessionOptions.GetQuestionsUsed(conversation.MessageCount),
                QuestionsLimitCount = _sessionOptions.GetQuestionsTotal()
            },
            Messages = conversation.Messages.Select(message => message.ToDexChatMessage()).ToList()
        };
    }

    /// <inheritdoc/>
    public Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken)
        => _store.ListAsync(userId, options, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string userId, Guid conversationId, CancellationToken cancellationToken) {
        var deleted = await _store.DeleteAsync(conversationId, userId, cancellationToken);
        return deleted > 0;
    }
}
