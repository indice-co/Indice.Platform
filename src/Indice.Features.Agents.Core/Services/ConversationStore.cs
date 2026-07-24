using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class ConversationStore : IConversationStore
{
    private readonly AgentsDbContext _db;
    private readonly SessionOptions _sessionOptions;

    /// <summary>Creates a new <see cref="ConversationStore"/>.</summary>
    public ConversationStore(AgentsDbContext db, IOptions<AgentsOptions> options) {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _sessionOptions = options.Value.Session;
    }

    /// <inheritdoc/>
    public async Task<Conversation?> LoadOrCreateAsync(string userId, Guid? conversationId, CancellationToken cancellationToken) {
        if (conversationId is null) {
            var now = DateTimeOffset.UtcNow;
            var entity = new DbConversation {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = null,
                CreatedAt = now,
                LastActivityAt = now,
                InputTokenCount = 0,
                OutputTokenCount = 0,
            };
            _db.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return ToDto(entity, messages: []);
        }

        // Metadata only: the send paths need identity/ownership; history is loaded during the run by the
        // pipeline'c chat-history provider.
        return await _db.Conversations
            .AsNoTracking()
            .Where(s => s.Id == conversationId!.Value && s.UserId == userId)
            .Select(s => new Conversation {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                InputTokenCount = s.InputTokenCount,
                OutputTokenCount = s.OutputTokenCount,
                MessageCount = s.MessageCount,
                Pin = s.Pin
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Conversation?> GetAsync(Guid conversationId, string userId, CancellationToken cancellationToken) {
        // HistoryWindow counts turns (user+assistant pairs); each turn is two persisted rows.
        var messageTake = _sessionOptions.HistoryWindow * 2;
        // Inlined (not SessionOptions.GetQuestionsUsed) because EF cannot translate the helper call.
        var questionsTotal = _sessionOptions.GetQuestionsTotal();
        var conversation = await _db.Conversations
            .AsNoTracking()
            .Where(s => s.Id == conversationId && s.UserId == userId)
            .Select(s => new Conversation {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                InputTokenCount = s.InputTokenCount,
                OutputTokenCount = s.OutputTokenCount,
                MessageCount = s.MessageCount,
                QuestionsUsedCount = questionsTotal == null ? null : ((s.MessageCount + 1) / 2 < questionsTotal ? (s.MessageCount + 1) / 2 : questionsTotal),
                QuestionsLimitCount = questionsTotal,
                Pin = s.Pin,
                Messages = s.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(messageTake)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessage {
                        MessageId = m.Id.ToString(),
                        Role = m.Role,
                        Contents = m.Contents,
                        CreatedAt = m.CreatedAt,
                        AuthorName = m.AuthorName,
                        AdditionalProperties = GetMessageAdditionalProperties(m.Liked)
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return conversation;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, CancellationToken cancellationToken) {
        // HistoryWindow counts turns (user+assistant pairs); each turn is two persisted rows.
        var messageTake = _sessionOptions.HistoryWindow * 2;
        var messages = await _db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(messageTake)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage {
                MessageId = m.Id.ToString(),
                Role = m.Role,
                Contents = m.Contents,
                CreatedAt = m.CreatedAt,
                AuthorName = m.AuthorName,
                AdditionalProperties = GetMessageAdditionalProperties(m.Liked)
            })
            .ToListAsync(cancellationToken);
        return messages;
    }

    /// <inheritdoc/>
    public async Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken) {
        var query = _db.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Pin)
            .ThenByDescending(c => c.LastActivityAt)
            .Select(c => new ConversationListItem {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                LastActivityAt = c.LastActivityAt,
                TotalPromptTokens = c.InputTokenCount,
                TotalCompletionTokens = c.OutputTokenCount,
                Pin = c.Pin
            });
        return await query.ToResultSetAsync(options, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ChatMessage> AppendTurnAsync(Guid conversationId, ChatMessage userMessage, ChatResponse response,
        CancellationToken cancellationToken) {

        var conversation = await _db.Conversations.FirstAsync(s => s.Id == conversationId, cancellationToken);
        var userDisplayName = await _db.Profiles.Where(x => x.UserId == conversation.UserId).Select(x => x.DisplayName).FirstOrDefaultAsync(cancellationToken);
        userMessage.AuthorName ??= userDisplayName;
        var userRow = ToDb(conversationId, userMessage, responseId: null, prompt: null, completion: null, model: null);
        var assistantRow = ToDb(conversationId, 
                                response.Messages.First(), 
                                responseId: response.ResponseId, 
                                prompt: (int)(response.Usage?.InputTokenCount ?? 0), 
                                completion: (int)(response.Usage?.OutputTokenCount ?? 0), 
                                model: string.IsNullOrWhiteSpace(response.ModelId) ? null : response.ModelId);
        _db.Add(userRow);
        _db.Add(assistantRow);

        conversation.LastActivityAt = assistantRow.CreatedAt;
        conversation.InputTokenCount += response.Usage?.InputTokenCount ?? 0;
        conversation.OutputTokenCount += response.Usage?.OutputTokenCount ?? 0;
        conversation.MessageCount += 2;
        if (conversation.Title is null && _sessionOptions.TitleAutoGenerate) {
            conversation.Title = DeriveTitle(userMessage);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatMessage {
            MessageId = assistantRow.Id.ToString(),
            Role = assistantRow.Role,
            Contents = assistantRow.Contents,
            CreatedAt = assistantRow.CreatedAt,
            AuthorName = assistantRow.AuthorName,
        };
    }

    /// <inheritdoc/>
    public async Task AppendFailedTurnAsync(Guid conversationId, ChatMessage userMessage, CancellationToken cancellationToken) {
        var session = await _db.Conversations.FirstAsync(s => s.Id == conversationId, cancellationToken);
        var userRow = ToDb(conversationId, userMessage, responseId: null, prompt: null, completion: null, model: null);
        _db.Add(userRow);
        session.LastActivityAt = userRow.CreatedAt;
        session.MessageCount += 1;
        if (session.Title is null && _sessionOptions.TitleAutoGenerate) {
            session.Title = DeriveTitle(userMessage);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAsync(Guid conversationId, string userId, CancellationToken cancellationToken) {
        var owned = await _db.Conversations
            .AsNoTracking()
            .AnyAsync(s => s.Id == conversationId && s.UserId == userId, cancellationToken);
        if (!owned) {
            return 0;
        }
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .ExecuteDeleteAsync(cancellationToken);
        var deleted = await _db.Conversations
            .Where(s => s.Id == conversationId && s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return deleted;
    }

    /// <inheritdoc/>
    public Task<int> CountSessionsAsync(string userId, CancellationToken cancellationToken)
        => _db.Conversations
            .AsNoTracking()
            .CountAsync(s => s.UserId == userId, cancellationToken);

    /// <inheritdoc/>
    public async Task<long> GetUsageTokensAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken) {
        // Per-turn reasoning tokens live on the assistant message rows; sum them across the user'c sessions
        // within the window. Nullable sum so an empty window materializes as 0 rather than throwing.
        var userConversationIds = _db.Conversations.Where(s => s.UserId == userId).Select(s => s.Id);
        return await _db.Messages
            .AsNoTracking()
            .Where(m => m.CreatedAt >= since && userConversationIds.Contains(m.ConversationId))
            .Select(m => (long?)((m.PromptTokens ?? 0) + (m.CompletionTokens ?? 0)))
            .SumAsync(cancellationToken) ?? 0;
    }

    /// <inheritdoc/>
    public async Task<bool> SetLikeAsync(string userId, Guid conversationId, Guid messageId, bool? liked, CancellationToken cancellationToken) {
        var owned = await _db.Conversations
            .AsNoTracking()
            .AnyAsync(s => s.Id == conversationId && s.UserId == userId, cancellationToken);
        if (!owned) {
            return false;
        }
        var affectedRows = await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.Id == messageId && m.Role == ChatRole.Assistant)
            .ExecuteUpdateAsync(m => m.SetProperty(msg => msg.Liked, liked), cancellationToken);
        return affectedRows > 0;
    }

    private static DbMessage ToDb(Guid conversationId, ChatMessage m, string? responseId, int? prompt, int? completion, string? model) => new() {
        Id = string.IsNullOrWhiteSpace(m.MessageId) || !Guid.TryParse(m.MessageId, out var parsedId) ? Guid.NewGuid() : parsedId,
        ConversationId = conversationId,
        ResponseId = responseId,
        Role = m.Role,
        Contents = m.Contents.ToList(),
        CreatedAt = m.CreatedAt ?? DateTimeOffset.UtcNow,
        PromptTokens = prompt,
        CompletionTokens = completion,
        ModelUsed = model,
        AuthorName = m.AuthorName,
        MetadataJson = null
    };

    private Conversation ToDto(DbConversation s, IReadOnlyList<ChatMessage> messages) => new() {
        Id = s.Id,
        Title = s.Title,
        CreatedAt = s.CreatedAt,
        LastActivityAt = s.LastActivityAt,
        InputTokenCount = s.InputTokenCount,
        OutputTokenCount = s.OutputTokenCount,
        MessageCount = s.MessageCount,
        QuestionsUsedCount = _sessionOptions.GetQuestionsUsed(s.MessageCount),
        QuestionsLimitCount = _sessionOptions.GetQuestionsTotal(),
        Messages = messages,
    };

    private static string DeriveTitle(ChatMessage firstUserMessage) {
        var normalized = firstUserMessage.Text.Replace('\r', ' ').Replace('\n', ' ').Trim() ?? string.Empty;
        return normalized.Length <= 80 ? normalized : normalized[..80];
    }

    private static AdditionalPropertiesDictionary GetMessageAdditionalProperties(bool? liked) { 
        return new AdditionalPropertiesDictionary() {
            [nameof(DbMessage.Liked)] = liked
        };
    }
}
