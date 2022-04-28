using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions
{
    /// <summary>A service that contains message type related operations.</summary>
    public interface IMessageTypeService
    {
        /// <summary>Gets a list of all message types in the system.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        Task<ResultSet<MessageType>> GetList(ListOptions options);
        /// <summary>
        /// Gets a message type by it's unique id.</summary>
        /// <param name="id">The id of the message type.</param>
        Task<MessageType> GetById(Guid id);
        /// <summary>Gets a message type by it's name.</summary>
        /// <param name="name">The name of the message type.</param>
        Task<MessageType> GetByName(string name);
        /// <summary>Creates a new contact.</summary>
        /// <param name="request">The data for the message type to create.</param>
        Task<MessageType> Create(UpsertMessageTypeRequest request);
        /// <summary>Updates an existing message type.</summary>
        /// <param name="id">The id of the message type.</param>
        /// <param name="request">The data for the message type to update.</param>
        Task Update(Guid id, UpsertMessageTypeRequest request);
        /// <summary>Deletes an existing message type.</summary>
        /// <param name="id">The id of the message type.</param>
        Task Delete(Guid id);
    }
}
