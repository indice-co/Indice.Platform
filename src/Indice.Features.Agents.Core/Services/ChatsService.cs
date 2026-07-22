using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ChatsService> _logger;

    // User-displayable reason for the terminal error event. Exception details never reach the wire — they are logged.
    private const string GenericFailureReason = "The assistant could not complete this request. Please try again.";

    /// <summary>Creates a new <see cref="ChatsService"/>.</summary>
    public ChatsService(IConversationStore store, IDexChatClient dexClient, IUsageGuardService usageGuard, IOptions<AgentsOptions> options, ILogger<ChatsService> logger) {
        _store = store;
        _dexClient = dexClient;
        _usageGuard = usageGuard;
        _deployments = options.Value.AzureOpenAI.Deployments;
        _sessionOptions = options.Value.Session;
        _logger = logger;
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
            return CreateLimitReachedResponse(conversation, turnCheck.Message);
        }
        var userMessage = new ChatMessage(ChatRole.User, chatRequest.Text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var response = await _dexClient.GetResponseAsync(userMessage, new ChatOptions { ConversationId = conversation.Id.ToString() }, cancellationToken);
        var persisted = await _store.AppendTurnAsync(conversation.Id, userMessage, response, cancellationToken);
        return CreateTurnResponse(conversation, response, persisted);
    }

    /// <summary>Builds the canonical limit-blocked response shared by the streaming and non-streaming paths.</summary>
    private DexChatResponse CreateLimitReachedResponse(Conversation conversation, string? message) => new() {
        ConversationId = conversation.Id,
        ResponseId = Guid.NewGuid().ToString(),
        Messages = [new DexChatMessage {
            MessageId = Guid.Empty.ToString(),
            Role = DexChatRole.Assistant,
            Content = new ChatMessageContent(message ?? string.Empty),
            CreatedAt = DateTimeOffset.UtcNow
        }],
        FinishReason = DexChatFinishReason.Limit,
        LimitReached = true,
        Usage = new DexChatUsage {
            QuestionsUsedCount = _sessionOptions.GetQuestionsUsed(conversation.MessageCount),
            QuestionsLimitCount = _sessionOptions.GetQuestionsTotal()
        }
    };

    /// <summary>Maps a completed turn to the canonical boundary response: stamps the conversation id, the persisted assistant message id, and the question counters.</summary>
    private DexChatResponse CreateTurnResponse(Conversation conversation, ChatResponse response, ChatMessage persisted) {
        var dexResponse = response.ToDexChatResponse();
        dexResponse.ConversationId ??= conversation.Id;
        if (dexResponse.Messages.LastOrDefault() is { } lastMessage) {
            lastMessage.MessageId = persisted.MessageId;
        }
        dexResponse.Usage ??= new DexChatUsage();
        dexResponse.Usage.QuestionsUsedCount = _sessionOptions.GetQuestionsUsed(conversation.MessageCount + 2);
        dexResponse.Usage.QuestionsLimitCount = _sessionOptions.GetQuestionsTotal();
        return dexResponse;
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<SseItem<DexChatResponseUpdate>>?> SendStreamAsync(string userId, Guid? conversationId, string text, CancellationToken cancellationToken) {
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

    /// <summary>Streams a blocked turn through the normal grammar — <c>start</c>, one <c>delta</c> carrying the guard's predefined reply, <c>usage</c>, then <c>done</c> with <c>finishReason</c> <c>limit</c>. Nothing is persisted.</summary>
    private async IAsyncEnumerable<SseItem<DexChatResponseUpdate>> LimitReachedStream(Conversation conversation, string? message) {
        var response = CreateLimitReachedResponse(conversation, message);
        yield return Message(new DexChatStreamStart { ConversationId = conversation.Id });
        if (response.Text is { Length: > 0 } text) {
            yield return Delta(text);
        }
        foreach (var frame in CompletionFrames(response)) {
            yield return frame;
        }
        await Task.CompletedTask;
    }

    /// <summary>Streams the turn as message/delta frames: <c>start</c>, a <c>status</c> per pipeline step, a <c>delta</c> per token, then the completion parts (<c>citations</c>/<c>sources</c>/<c>usage</c>) and terminal <c>done</c> — or a terminal <c>error</c> on failure (user message persisted, question counted).</summary>
    private async IAsyncEnumerable<SseItem<DexChatResponseUpdate>> StreamTurnAsync(
        Conversation conversation, string text, [EnumeratorCancellation] CancellationToken cancellationToken) {

        var userMessage = new ChatMessage(ChatRole.User, text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var stream = _dexClient.GetStreamingResponseAsync(userMessage, new ChatOptions { ConversationId = conversation.Id.ToString() }, cancellationToken);
        var updates = new List<ChatResponseUpdate>();
        var failed = false;

        yield return Message(new DexChatStreamStart { ConversationId = conversation.Id });

        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        while (!failed) {
            ChatResponseUpdate evt;
            try {
                if (!await enumerator.MoveNextAsync()) {
                    break;
                }
                evt = enumerator.Current;
            } catch (OperationCanceledException) {
                throw; // The client went away — nothing left to emit.
            } catch (Exception exception) {
                _logger.LogError(exception, "Chat pipeline stream faulted for conversation {ConversationId}.", conversation.Id);
                failed = true;
                break;
            }
            if (evt.Contents.OfType<ErrorContent>().FirstOrDefault() is { } error) {
                _logger.LogError("Chat pipeline failed for conversation {ConversationId}: {Reason}", conversation.Id, error.Message);
                failed = true;
                break;
            }
            // Progress updates are ephemeral streaming UI hints and never part of the aggregated answer.
            if (evt.Contents.OfType<StepProgressContent>().FirstOrDefault() is { } stepUpdate) {
                yield return Message(new DexChatStreamStatus { Value = stepUpdate.Label });
                continue;
            }
            updates.Add(evt);
            if (!string.IsNullOrEmpty(evt.Text)) {
                yield return Delta(evt.Text);
            }
        }

        var response = failed ? null : updates.ToChatResponse();
        if (response is not null && response.Messages.Count == 0) {
            _logger.LogError("Chat pipeline produced no assistant message for conversation {ConversationId}.", conversation.Id);
            response = null;
        }

        if (response is null) {
            await _store.AppendFailedTurnAsync(conversation.Id, userMessage, cancellationToken);
            yield return Message(new DexChatStreamError { Reason = GenericFailureReason });
            yield break;
        }

        var persisted = await _store.AppendTurnAsync(conversation.Id, userMessage, response, cancellationToken);
        foreach (var frame in CompletionFrames(CreateTurnResponse(conversation, response, persisted))) {
            yield return frame;
        }
    }

    /// <summary>Wraps a non-delta frame for the default <c>message</c> SSE event.</summary>
    private static SseItem<DexChatResponseUpdate> Message(DexChatResponseUpdate frame) => new(frame);

    /// <summary>Wraps answer text for the <c>delta</c> SSE event.</summary>
    private static SseItem<DexChatResponseUpdate> Delta(string text) => new(new DexChatStreamDelta { Text = text }, "delta");

    /// <summary>Decomposes a canonical response into its completion frames: <c>citations</c>/<c>sources</c> when non-empty, <c>usage</c>, then the terminal <c>done</c> with the metadata that was not streamed as text.</summary>
    private static IEnumerable<SseItem<DexChatResponseUpdate>> CompletionFrames(DexChatResponse response) {
        var message = response.Messages.LastOrDefault();
        if (message?.Citations is { Count: > 0 } citations) {
            yield return Message(new DexChatStreamCitations { Value = citations });
        }
        if (message?.Sources is { Count: > 0 } sources) {
            yield return Message(new DexChatStreamSources { Value = sources });
        }
        if (response.Usage is not null) {
            yield return Message(new DexChatStreamUsage { Value = response.Usage });
        }
        yield return Message(new DexChatStreamDone {
            MessageId = message?.MessageId,
            ResponseId = response.ResponseId,
            ModelId = response.ModelId,
            CreatedAt = response.CreatedAt,
            FinishReason = response.FinishReason,
            LimitReached = response.LimitReached
        });
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

    /// <inheritdoc/>
    public Task<bool> SetLikeAsync(string userId, Guid conversationId, Guid messageId, bool? liked, CancellationToken cancellationToken) {
        return _store.SetLikeAsync(userId, conversationId, messageId, liked, cancellationToken);
    }
}
