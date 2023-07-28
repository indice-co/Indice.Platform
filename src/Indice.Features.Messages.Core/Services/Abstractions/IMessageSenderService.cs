using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions;
/// <summary>A service that contains message sender related operations.</summary>
public interface IMessageSenderService
{
    /// <summary>Gets a list of all message senders in the system.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <param name="filter">The filter applied to limit the results.</param>
    Task<ResultSet<MessageSender>> GetList(ListOptions options, MessageSenderListFilter filter);
    /// <summary>
    /// Gets a message sender by it's unique id.</summary>
    /// <param name="id">The id of the message sender.</param>
    Task<MessageSender> GetById(Guid id);
    /// <summary>Gets a message sender by it's name.</summary>
    /// <param name="name">The name of the message sender.</param>
    Task<MessageSender> GetByName(string name);
    /// <summary>Creates a new contact.</summary>
    /// <param name="request">The data for the message sender to create.</param>
    Task<MessageSender> Create(CreateMessageSenderRequest request);
    /// <summary>Updates an existing message sender.</summary>
    /// <param name="id">The id of the message sender.</param>
    /// <param name="request">The data for the message sender to update.</param>
    Task Update(Guid id, UpdateMessageSenderRequest request);
    /// <summary>Deletes an existing message sender.</summary>
    /// <param name="id">The id of the message sender.</param>
    Task Delete(Guid id);
}
