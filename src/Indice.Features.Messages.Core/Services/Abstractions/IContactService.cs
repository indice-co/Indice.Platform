using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions
{
    /// <summary>A service that contains contact related operations.</summary>
    public interface IContactService
    {
        /// <summary>Gets a list of all contacts in the system.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        Task<ResultSet<Contact>> GetList(ListOptions<ContactListFilter> options);
        /// <summary>Gets a contact by it's unique id.</summary>
        /// <param name="id">The id of the contact.</param>
        Task<Contact> GetById(Guid id);
        /// <summary>Adds a contact to an existing distribution list.</summary>
        /// <param name="id">The id of the distribution list.</param>
        /// <param name="request">The data for the contact to add.</param>
        /// <remarks>This method will also create the contact if the a contact with the given id is not found or the id is null</remarks>
        Task AddToDistributionList(Guid id, CreateDistributionListContactRequest request);
        /// <summary>Creates a new contact.</summary>
        /// <param name="request">The data for the contact to create.</param>
        Task<Contact> Create(CreateContactRequest request);
        /// <summary>Creates multiple contacts in the store.</summary>
        /// <param name="contacts">The data for the contacts to create.</param>
        Task CreateMany(IEnumerable<CreateContactRequest> contacts);
        /// <summary>Updates an existing contact.</summary>
        /// <param name="id">The id of the contact.</param>
        /// <param name="request">The data for the contact to update.</param>
        Task Update(Guid id, UpdateContactRequest request);
        /// <summary>>Removes an existing contact from the specified distribution list.</summary>
        /// <param name="id">The id of the distribution list.</param>
        /// <param name="contactId">The unique id of the contact.</param>
        Task RemoveFromDistributionList(Guid id, Guid contactId);
    }

    /// <summary>
    /// Extensions on the <see cref="IContactService"/>
    /// </summary>
    public static class IContactServiceExtensions
    {
        /// <summary>
        /// Searches and finds the first contact found in the store by its <paramref name="email"/>.
        /// </summary>
        /// <param name="contactService">The <see cref="IContactService"/> to extend</param>
        /// <param name="email">The contact email to search by</param>
        /// <returns></returns>
        public async static Task<Contact> FindByEmail(this IContactService contactService, string email) {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));
            var options = new ListOptions<ContactListFilter> { Size = 1 }; // top 1 results equals first or default.
            options.Filter.Email = email;
            return (await contactService.GetList(options)).Items.FirstOrDefault();
        }
        /// <summary>
        /// Searches and finds the first contact found in the store by its <paramref name="phoneNumber"/>.
        /// </summary>
        /// <param name="contactService">The <see cref="IContactService"/> to extend</param>
        /// <param name="phoneNumber">The contact email to search by</param>
        /// <returns></returns>
        public async static Task<Contact> FindByPhoneNumber(this IContactService contactService, string phoneNumber) {
            if (string.IsNullOrEmpty(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));
            var options = new ListOptions<ContactListFilter> { Size = 1 }; // top 1 results equals first or default.
            options.Filter.PhoneNumber = phoneNumber;
            return (await contactService.GetList(options)).Items.FirstOrDefault();
        }



        /// <summary>Gets a contact by it's recipient id.</summary>
        /// <param name="contactService">The <see cref="IContactService"/> to extend</param>
        /// <param name="recipientId">The id of the recipient.</param>
        /// <returns></returns>
        public async static Task<Contact> FindByRecipientId(this IContactService contactService, string recipientId) {
            if (string.IsNullOrEmpty(recipientId))
                throw new ArgumentNullException(nameof(recipientId));
            var options = new ListOptions<ContactListFilter> { Size = 1 }; // top 1 results equals first or default.
            options.Filter.RecipientId = recipientId;
            return (await contactService.GetList(options)).Items.FirstOrDefault();
        }
    }
}
