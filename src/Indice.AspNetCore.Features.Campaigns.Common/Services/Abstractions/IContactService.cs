using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// A service that contains contact related operations.
    /// </summary>
    public interface IContactService
    {
        /// <summary>
        /// Gets a list of all contacts in the system.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        Task<ResultSet<Contact>> GetList(ListOptions options);
        /// <summary>
        /// Gets a contact by it's unique id.
        /// </summary>
        /// <param name="id">The id of the contact.</param>
        Task<Contact> GetById(Guid id);
        /// <summary>
        /// Gets a contact by it's recipient id.
        /// </summary>
        /// <param name="id">The id of the recipient.</param>
        Task<Contact> GetByRecipientId(string id);
        /// <summary>
        /// Adds a contact to an existing distribution list.
        /// </summary>
        /// <param name="id">The id of the distribution list.</param>
        /// <param name="request">The data for the contact to add.</param>
        Task AddToDistributionList(Guid id, CreateDistributionListContactRequest request);
        /// <summary>
        /// Creates a new contact.
        /// </summary>
        /// <param name="request">The data for the contact to create.</param>
        Task<Contact> Create(CreateContactRequest request);
        /// <summary>
        /// Creates multiple contacts in the store.
        /// </summary>
        /// <param name="contacts">The data for the contacts to create.</param>
        Task CreateMany(IEnumerable<CreateContactRequest> contacts);
        /// <summary>
        /// Updates an existing contact.
        /// </summary>
        /// <param name="id">The id of the contact.</param>
        /// <param name="request">The data for the contact to update.</param>
        Task Update(Guid id, UpdateContactRequest request);
    }
}
