using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class SessionsStore : ISessionsStore
{
    private readonly AgentsDbContext _db;
    private readonly SessionOptions _sessionOptions;

    /// <summary>Creates a new <see cref="SessionsStore"/>.</summary>
    public SessionsStore(AgentsDbContext db, IOptions<AgentsOptions> options) {
        _db = db;
        _sessionOptions = options.Value.Session;
    }

    /// <inheritdoc/>
    public async Task<Session?> LoadOrCreateAsync(string userId, Guid? sessionId, CancellationToken cancellationToken) {
        if (sessionId is null) {
            var now = DateTimeOffset.UtcNow;
            var entity = new DbSession {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = null,
                CreatedAt = now,
                LastActivityAt = now,
                TotalPromptTokens = 0,
                TotalCompletionTokens = 0,
            };
            _db.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return ToDto(entity, messages: []);
        }

        // Metadata only: the send paths need identity/ownership; history is loaded during the run by the
        // pipeline's chat-history provider.
        return await _db.Set<DbSession>()
            .AsNoTracking()
            .Where(s => s.Id == sessionId.Value && s.UserId == userId)
            .Select(s => new Session {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                TotalPromptTokens = s.TotalPromptTokens,
                TotalCompletionTokens = s.TotalCompletionTokens,
                MessageCount = s.MessageCount,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Session?> GetAsync(Guid sessionId, string userId, CancellationToken cancellationToken) {
        // HistoryWindow counts turns (user+assistant pairs); each turn is two persisted rows.
        var messageTake = _sessionOptions.HistoryWindow * 2;
        // Inlined (not SessionOptions.GetQuestionsUsed) because EF cannot translate the helper call.
        var questionsTotal = _sessionOptions.GetQuestionsTotal();
        var session = await _db.Set<DbSession>()
            .AsNoTracking()
            .Where(s => s.Id == sessionId && s.UserId == userId)
            .Select(s => new Session {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                TotalPromptTokens = s.TotalPromptTokens,
                TotalCompletionTokens = s.TotalCompletionTokens,
                MessageCount = s.MessageCount,
                QuestionsUsed = questionsTotal == null ? null : (s.MessageCount / 2 < questionsTotal ? s.MessageCount / 2 : questionsTotal),
                QuestionsTotal = questionsTotal,
                Messages = s.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(messageTake)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessage {
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return session;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid sessionId, CancellationToken cancellationToken) {
        // HistoryWindow counts turns (user+assistant pairs); each turn is two persisted rows.
        var messageTake = _sessionOptions.HistoryWindow * 2;
        return await _db.Set<DbSessionMessage>()
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(messageTake)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ResultSet<SessionListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken) {
        var query = _db.Set<DbSession>()
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new SessionListItem {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                TotalPromptTokens = s.TotalPromptTokens,
                TotalCompletionTokens = s.TotalCompletionTokens,
            });
        return await query.ToResultSetAsync(options, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ChatMessage> AppendTurnAsync(Guid sessionId, ChatMessage userMessage, ChatMessage assistantMessage,
        long promptTokens, long completionTokens, string? modelUsed, CancellationToken cancellationToken) {

        var session = await _db.Set<DbSession>().FirstAsync(s => s.Id == sessionId, cancellationToken);
        var userRow = ToDb(sessionId, userMessage, prompt: null, completion: null, model: null);
        var assistantRow = ToDb(sessionId, assistantMessage, prompt: (int)promptTokens, completion: (int)completionTokens, model: modelUsed);
        _db.Add(userRow);
        _db.Add(assistantRow);

        session.LastActivityAt = assistantMessage.CreatedAt;
        session.TotalPromptTokens += promptTokens;
        session.TotalCompletionTokens += completionTokens;
        session.MessageCount += 2;
        if (session.Title is null && _sessionOptions.TitleAutoGenerate) {
            session.Title = DeriveTitle(userMessage.Content);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatMessage {
            Id = assistantRow.Id,
            Role = assistantRow.Role,
            Content = assistantRow.Content,
            CreatedAt = assistantRow.CreatedAt,
        };
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAsync(Guid sessionId, string userId, CancellationToken cancellationToken) {
        var owned = await _db.Set<DbSession>()
            .AsNoTracking()
            .AnyAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (!owned) {
            return 0;
        }
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Set<DbSessionMessage>()
            .Where(m => m.SessionId == sessionId)
            .ExecuteDeleteAsync(cancellationToken);
        var deleted = await _db.Set<DbSession>()
            .Where(s => s.Id == sessionId && s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return deleted;
    }

    /// <inheritdoc/>
    public Task<int> CountSessionsAsync(string userId, CancellationToken cancellationToken)
        => _db.Set<DbSession>()
            .AsNoTracking()
            .CountAsync(s => s.UserId == userId, cancellationToken);

    /// <inheritdoc/>
    public async Task<long> GetUsageTokensAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken) {
        // Per-turn reasoning tokens live on the assistant message rows; sum them across the user's sessions
        // within the window. Nullable sum so an empty window materializes as 0 rather than throwing.
        var userSessionIds = _db.Set<DbSession>().Where(s => s.UserId == userId).Select(s => s.Id);
        return await _db.Set<DbSessionMessage>()
            .AsNoTracking()
            .Where(m => m.CreatedAt >= since && userSessionIds.Contains(m.SessionId))
            .Select(m => (long?)((m.PromptTokens ?? 0) + (m.CompletionTokens ?? 0)))
            .SumAsync(cancellationToken) ?? 0;
    }

    private static DbSessionMessage ToDb(Guid sessionId, ChatMessage m, int? prompt, int? completion, string? model) => new() {
        Id = m.Id == Guid.Empty ? Guid.NewGuid() : m.Id,
        SessionId = sessionId,
        Role = m.Role,
        Content = m.Content,
        CreatedAt = m.CreatedAt == default ? DateTimeOffset.UtcNow : m.CreatedAt,
        PromptTokens = prompt,
        CompletionTokens = completion,
        ModelUsed = model,
        MetadataJson = null,
    };

    private Session ToDto(DbSession s, IReadOnlyList<ChatMessage> messages) => new() {
        Id = s.Id,
        Title = s.Title,
        CreatedAt = s.CreatedAt,
        LastActivityAt = s.LastActivityAt,
        TotalPromptTokens = s.TotalPromptTokens,
        TotalCompletionTokens = s.TotalCompletionTokens,
        MessageCount = s.MessageCount,
        QuestionsUsed = _sessionOptions.GetQuestionsUsed(s.MessageCount),
        QuestionsTotal = _sessionOptions.GetQuestionsTotal(),
        Messages = messages,
    };

    private static string DeriveTitle(string firstUserMessage) {
        var normalized = firstUserMessage.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= 80 ? normalized : normalized[..80];
    }
}
