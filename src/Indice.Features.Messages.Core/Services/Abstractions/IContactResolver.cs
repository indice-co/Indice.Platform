using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions
{
    /// <summary>Contains information that help gather contact information from other systems.</summary>
    public interface IContactResolver
    {
        /// <summary>Specifies a way to resolve a contact from an external system.</summary>
        /// <param name="id">The unique id of the contact.</param>
        Task<Contact> GetById(string id);
        /// <summary>Searches a list of contacts, using the specified criteria, from an external system.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        Task<ResultSet<Contact>> Find(ListOptions options);
    }
}
