using System.Security.Cryptography;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Stores the files an agent produces during a conversation, and hands them back to the conversation's owner.</summary>
public interface IConversationFileStore
{
    /// <summary>Saves a file in a conversation.</summary>
    /// <param name="conversationId">The conversation the file belongs to. It must exist.</param>
    /// <param name="userId">The owner of the conversation.</param>
    /// <param name="fileName">The name to download it as. Path separators and control characters are removed.</param>
    /// <param name="contentType">The media type.</param>
    /// <param name="data">The content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reference to put in a message part.</returns>
    Task<FileReference> SaveAsync(Guid conversationId, string userId, string fileName, string contentType, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file. The user id and the conversation id both have to match, so another user, or the right user asking through
    /// another conversation, gets <c>null</c> exactly as for a file that does not exist.
    /// </summary>
    /// <param name="conversationId">The conversation the file belongs to.</param>
    /// <param name="fileId">The file id.</param>
    /// <param name="userId">The requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<StoredFile?> GetAsync(Guid conversationId, Guid fileId, string userId, CancellationToken cancellationToken = default);
}

/// <inheritdoc/>
public class ConversationFileStore : IConversationFileStore
{
    /// <summary>The largest file the store accepts. A report is a few hundred kilobytes; this stops a runaway producer.</summary>
    public const int MaxFileBytes = 20 * 1024 * 1024;

    private const int MaxFileNameLength = 200;

    private readonly AgentsDbContext _db;

    /// <summary>Creates a new <see cref="ConversationFileStore"/>.</summary>
    public ConversationFileStore(AgentsDbContext db) {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<FileReference> SaveAsync(Guid conversationId, string userId, string fileName, string contentType, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (conversationId == Guid.Empty) {
            throw new ArgumentException("A file belongs to a conversation.", nameof(conversationId));
        }
        if (data.Length > MaxFileBytes) {
            throw new ArgumentException($"The file is {data.Length} bytes; the limit is {MaxFileBytes}.", nameof(data));
        }
        var entity = new DbFile {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = userId,
            FileName = SafeFileName(fileName),
            ContentType = contentType.Trim(),
            Length = data.Length,
            Sha256 = Convert.ToHexString(SHA256.HashData(data.Span)).ToLowerInvariant(),
            Data = data.ToArray(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Set<DbFile>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToReference(entity);
    }

    /// <inheritdoc/>
    public async Task<StoredFile?> GetAsync(Guid conversationId, Guid fileId, string userId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return null;
        }
        var entity = await _db.Set<DbFile>()
                              .AsNoTracking()
                              .FirstOrDefaultAsync(file => file.Id == fileId && file.ConversationId == conversationId && file.UserId == userId, cancellationToken);
        return entity is null ? null : new StoredFile(ToReference(entity), entity.Data, entity.CreatedAt);
    }

    private static FileReference ToReference(DbFile entity) => new() {
        Id = entity.Id,
        FileName = entity.FileName,
        ContentType = entity.ContentType,
        Length = entity.Length,
        Sha256 = entity.Sha256
    };

    // The name ends up in a Content-Disposition header. Keep a file name, never a path, and nothing that can break the header.
    private static string SafeFileName(string? fileName) {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        var clean = new string([.. name.Where(c => !char.IsControl(c) && c is not ('"' or ';' or '<' or '>' or '|' or ':' or '*' or '?'))]).Trim().TrimEnd('.');
        if (clean.Length > MaxFileNameLength) {
            var extension = Path.GetExtension(clean);
            clean = clean[..(MaxFileNameLength - extension.Length)] + extension;
        }
        return clean.Length == 0 ? "file" : clean;
    }
}
