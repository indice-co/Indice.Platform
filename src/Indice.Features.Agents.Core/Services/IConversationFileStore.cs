using Indice.Features.Agents.Core.Models;

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
