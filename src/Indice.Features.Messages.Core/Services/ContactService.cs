using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// An implementation of <see cref="IContactService"/> for Entity Framework Core.
    /// </summary>
    public class ContactService : IContactService
    {
        /// <summary>
        /// Creates a new instance of <see cref="ContactService"/>.
        /// </summary>
        /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ContactService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        private CampaignsDbContext DbContext { get; }

        /// <inheritdoc />
        public async Task AddToDistributionList(Guid id, CreateDistributionListContactRequest request) {
            DbContact contact;
            var list = await DbContext.DistributionLists.FindAsync(id);
            if (list is null) {
                throw CampaignException.DistributionListNotFound(id);
            }
            if (request.Id.HasValue) {
                contact = await DbContext.Contacts.FindAsync(request.Id.Value);
                if (contact is null) {
                    throw CampaignException.ContactNotFound(id);
                }
                contact.DistributionListId = list.Id;
                contact.Email = request.Email;
                contact.FirstName = request.FirstName;
                contact.FullName = request.FullName;
                contact.Id = request.Id ?? Guid.NewGuid();
                contact.LastName = request.LastName;
                contact.PhoneNumber = request.PhoneNumber;
                contact.RecipientId = request.RecipientId;
                contact.Salutation = request.Salutation;
                contact.UpdatedAt = DateTimeOffset.UtcNow;
                await DbContext.SaveChangesAsync();
                return;
            }
            contact = Mapper.ToDbContact(request);
            contact.DistributionListId = id;
            DbContext.Contacts.Add(contact);
            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<Contact> Create(CreateContactRequest request) {
            var contact = Mapper.ToDbContact(request);
            DbContext.Contacts.Add(contact);
            await DbContext.SaveChangesAsync();
            return Mapper.ToContact(contact);
        }

        /// <inheritdoc />
        public async Task CreateMany(IEnumerable<CreateContactRequest> contacts) {
            DbContext.Contacts.AddRange(contacts.Select(contact => Mapper.ToDbContact(contact)));
            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<Contact> GetById(Guid id) {
            var contact = await DbContext.Contacts.FindAsync(id);
            if (contact is null) {
                return default;
            }
            return Mapper.ToContact(contact);
        }

        /// <inheritdoc />
        public async Task<Contact> GetByRecipientId(string id) {
            var contact = await DbContext.Contacts.SingleOrDefaultAsync(x => x.RecipientId == id);
            if (contact is null) {
                return default;
            }
            return Mapper.ToContact(contact);
        }

        /// <inheritdoc />
        public async Task<ResultSet<Contact>> GetList(ListOptions<ContactListFilter> options) {
            var query = DbContext
                .Contacts
                .AsNoTracking();
            var filter = options.Filter;
            if (filter?.DistributionListId is not null) {
                query = query.Where(x => x.DistributionListId == filter.DistributionListId.Value);
            }
            return await query.Select(Mapper.ProjectToContact).ToResultSetAsync(options);
        }

        /// <inheritdoc />
        public async Task Update(Guid id, UpdateContactRequest request) {
            var contact = await DbContext.Contacts.FindAsync(id);
            if (contact is null) {
                throw CampaignException.ContactNotFound(id);
            }
            contact.Email = request.Email;
            contact.FirstName = request.FirstName;
            contact.FullName = request.FullName;
            contact.LastName = request.LastName;
            contact.PhoneNumber = request.PhoneNumber;
            contact.Salutation = request.Salutation;
            contact.UpdatedAt = DateTimeOffset.UtcNow;
            await DbContext.SaveChangesAsync();
        }
    }
}
