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
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorName = chatRequest.AuthorName
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
        //debateable
        if (dexResponse.Messages.LastOrDefault() is { } lastMessage) {
            lastMessage.MessageId = persisted.MessageId;
        }
        dexResponse.Usage ??= new DexChatUsage();
        dexResponse.Usage.QuestionsUsedCount = _sessionOptions.GetQuestionsUsed(conversation.MessageCount + 2);
        dexResponse.Usage.QuestionsLimitCount = _sessionOptions.GetQuestionsTotal();
        return dexResponse;
    }

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<SseItem<DexChatResponseUpdate>>?> SendStreamAsync(string userId, Guid? conversationId, ChatRequest chatRequest, CancellationToken cancellationToken) {
        await EnsureSessionCreationAllowedAsync(userId, conversationId, cancellationToken);
        var conversation = await _store.LoadOrCreateAsync(userId, conversationId, cancellationToken);
        if (conversation is null) {
            return null;
        }
        var turnCheck = _usageGuard.Check(conversation);
        if (!turnCheck.Allowed) {
            return LimitReachedStream(conversation, turnCheck.Message);
        }
        return StreamTurnAsync(conversation, chatRequest, cancellationToken);
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

    /// <summary>Streams a blocked turn through the normal grammar — <c>start</c>, the projected response as patch frames, then the bare <c>done</c>. Nothing is persisted.</summary>
    private async IAsyncEnumerable<SseItem<DexChatResponseUpdate>> LimitReachedStream(Conversation conversation, string? message) {
        var response = CreateLimitReachedResponse(conversation, message);
        yield return Message(new DexChatStreamStart { ConversationId = conversation.Id });
        var compactor = new DeltaCompactor();
        foreach (var frame in new DexChatStreamProjector().ProjectResponse(response)) {
            yield return compactor.Compact(frame);
        }
        yield return Message(new DexChatStreamDone());
        await Task.CompletedTask;
    }

    /// <summary>
    /// Streams the turn as message/delta frames: <c>start</c>, a <c>status</c> per pipeline progress hint, patch frames
    /// building the response document (token appends, atomic parts, then the completion tail after persistence), and the
    /// terminal bare <c>done</c> — or a terminal <c>error</c> on failure. Whatever ends the stream without a persisted
    /// turn (fault, disconnect), the user message is salvaged as a failed turn so the question stays in the conversation
    /// and counts toward the limit.
    /// </summary>
    private async IAsyncEnumerable<SseItem<DexChatResponseUpdate>> StreamTurnAsync(
        Conversation conversation, ChatRequest chatRequest, [EnumeratorCancellation] CancellationToken cancellationToken) {

        var userMessage = new ChatMessage(ChatRole.User, chatRequest.Text) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorName = chatRequest.AuthorName
        };
        var stream = _dexClient.GetStreamingResponseAsync(userMessage, new ChatOptions { ConversationId = conversation.Id.ToString() }, cancellationToken);
        var updates = new List<ChatResponseUpdate>();
        var projector = new DexChatStreamProjector();
        var compactor = new DeltaCompactor();
        var failed = false;
        var turnPersisted = false;

        yield return Message(new DexChatStreamStart { ConversationId = conversation.Id });

        try {
            foreach (var frame in projector.Begin(conversation.Id)) {
                yield return compactor.Compact(frame);
            }

            await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
            while (!failed) {
                ChatResponseUpdate evt;
                try {
                    if (!await enumerator.MoveNextAsync()) {
                        break;
                    }
                    evt = enumerator.Current;
                } catch (OperationCanceledException) {
                    throw; // The client went away — the finally below salvages the turn.
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
                // Progress labels are ephemeral UI hints: surface them, then strip them so they are neither aggregated nor persisted.
                foreach (var step in evt.Contents.OfType<StepProgressContent>().ToList()) {
                    yield return Message(new DexChatStreamStatus { Value = step.Label });
                    evt.Contents.Remove(step);
                }
                updates.Add(evt);
                foreach (var content in evt.Contents) {
                    switch (content) {
                        case TextContent text when !string.IsNullOrEmpty(text.Text):
                            foreach (var frame in projector.AppendText(text.Text)) {
                                yield return compactor.Compact(frame);
                            }
                            break;
                        case DataContent data:
                            foreach (var frame in projector.AddPart(ChatMessagePart.FromText(data.Uri, data.MediaType))) {
                                yield return compactor.Compact(frame);
                            }
                            break;
                    }
                }
            }

            var response = failed ? null : updates.ToChatResponse();
            if (response is not null && response.Messages.Count == 0) {
                _logger.LogError("Chat pipeline produced no assistant message for conversation {ConversationId}.", conversation.Id);
                response = null;
            }

            if (response is null) {
                await _store.AppendFailedTurnAsync(conversation.Id, userMessage, cancellationToken);
                turnPersisted = true;
                yield return Message(new DexChatStreamError { Reason = GenericFailureReason });
                yield break;
            }

            var persisted = await _store.AppendTurnAsync(conversation.Id, userMessage, response, cancellationToken);
            turnPersisted = true;
            foreach (var frame in projector.Complete(CreateTurnResponse(conversation, response, persisted))) {
                yield return compactor.Compact(frame);
            }
            yield return Message(new DexChatStreamDone());
        } 
        finally {
            if (!turnPersisted) {
                // Disconnect or fault before persistence: keep the user's question in the conversation and count it.
                await _store.AppendFailedTurnAsync(conversation.Id, userMessage, CancellationToken.None);
            }
        }
    }

    /// <summary>Wraps a non-delta frame for the default <c>message</c> SSE event.</summary>
    private static SseItem<DexChatResponseUpdate> Message(DexChatResponseUpdate frame) => new(frame);

    /// <summary>
    /// Per-stream frame compaction (wire concern only): nulls a delta's <c>path</c>/<c>op</c> when identical to the
    /// previous delta's effective values, so consecutive token appends shrink to <c>{"type":"delta","value":…}</c>.
    /// The first delta always goes out full. Clients inflate by carrying the last effective path/op forward.
    /// </summary>
    private sealed class DeltaCompactor
    {
        private string? _path;
        private DexChatPatchOp? _op;

        public SseItem<DexChatResponseUpdate> Compact(DexChatStreamDelta frame) {
            if (frame.Path == _path) { frame.Path = null; } else { _path = frame.Path; }
            if (frame.Op == _op) { frame.Op = null; } else { _op = frame.Op; }
            return new SseItem<DexChatResponseUpdate>(frame, "delta");
        }
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
