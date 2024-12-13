using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>A service that contains message related operations.</summary>
public interface IMessageService
{
    /// <summary>Gets an inbox message by it's unique id.</summary>
    /// <param name="id">The id of the inbox message.</param>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="channel">The channel of the message.</param>
    Task<Message?> GetById(Guid id, string recipientId, MessageChannelKind? channel = MessageChannelKind.Inbox);
    /// <summary>Gets a list of all inbox messages of a recipient in the system.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    Task<ResultSet<Message>?> GetList(string recipientId, ListOptions<MessagesFilter>? options);
    /// <summary>Marks an inbox message as read.</summary>
    /// <param name="id">The id of the inbox message.</param>
    /// <param name="recipientId">The id of the recipient.</param>
    Task MarkAsRead(Guid id, string recipientId);
    /// <summary>Marks an inbox message as deleted.</summary>
    /// <param name="id">The id of the inbox message.</param>
    /// <param name="recipientId">The id of the recipient.</param>
    Task MarkAsDeleted(Guid id, string recipientId);
    /// <summary>Creates a new inbox message.</summary>
    /// <param name="request">The data for the inbox message to create.</param>
    Task<Guid> Create(CreateMessageRequest request);
}
