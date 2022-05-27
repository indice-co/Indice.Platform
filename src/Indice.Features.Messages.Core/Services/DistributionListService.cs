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
    /// An implementation of <see cref="IDistributionListService"/> for Entity Framework Core.
    /// </summary>
    public class DistributionListService : IDistributionListService
    {
        /// <summary>
        /// Creates a new instance of <see cref="DistributionListService"/>.
        /// </summary>
        /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DistributionListService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        private CampaignsDbContext DbContext { get; }

        /// <inheritdoc />
        public async Task<DistributionList> Create(CreateDistributionListRequest request, IEnumerable<Contact> contacts = null) {
            var list = new DbDistributionList {
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = request.CreatedBy,
                Id = Guid.NewGuid(),
                Name = request.Name
            };
            // the following code will try at its best effort to create contacts
            // without making duplicates.
            // external contact resolution by recipient id is not need here.
            // It will happen down the line when this is published.
            if (contacts?.Any() == true) { 
                var recipientIds = contacts.Where(x => !string.IsNullOrEmpty(x.RecipientId)).Select(x => x.RecipientId).ToArray();
                var emails = contacts.Where(x => !string.IsNullOrEmpty(x.Email)).Select(x => x.Email).ToArray();
                var phones = contacts.Where(x => !string.IsNullOrEmpty(x.PhoneNumber)).Select(x => x.PhoneNumber).ToArray();
                var existingContacts = new Dictionary<string, List<DbContact>>();
                foreach (var item in contacts) {
                    var dbContact = default(DbContact);
                    if (!string.IsNullOrWhiteSpace(item.RecipientId)) {
                        if (!existingContacts.ContainsKey(nameof(item.RecipientId))) {
                            existingContacts[nameof(item.RecipientId)] = await DbContext.Contacts.Where(x => recipientIds.Contains(x.RecipientId)).ToListAsync();
                        }
                        dbContact = existingContacts[nameof(item.RecipientId)].Where(x => x.RecipientId == item.RecipientId).FirstOrDefault();
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Email)) {
                        if (!existingContacts.ContainsKey(nameof(item.Email))) {
                            existingContacts[nameof(item.Email)] = await DbContext.Contacts.Where(x => emails.Contains(x.Email)).ToListAsync();
                        }
                        dbContact = existingContacts[nameof(item.Email)].Where(x => x.Email.ToLower() == item.Email.ToLower()).FirstOrDefault();
                    } else if (!string.IsNullOrWhiteSpace(item.PhoneNumber)) {
                        if (!existingContacts.ContainsKey(nameof(item.PhoneNumber))) {
                            existingContacts[nameof(item.PhoneNumber)] = await DbContext.Contacts.Where(x => phones.Contains(x.PhoneNumber)).ToListAsync();
                        }
                        dbContact = existingContacts[nameof(item.PhoneNumber)].Where(x => x.PhoneNumber.ToLower() == item.PhoneNumber.ToLower()).FirstOrDefault();
                    }
                    if (dbContact is null) {
                        dbContact = Mapper.ToDbContact(item);
                        DbContext.Contacts.Add(dbContact);
                    }
                    list.ContactDistributionLists.Add(new DbDistributionListContact { 
                        Contact = dbContact,
                        ContactId = dbContact.Id
                    });
                }
            }
            DbContext.DistributionLists.Add(list);
            await DbContext.SaveChangesAsync();
            return new DistributionList {
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
                Id = list.Id,
                Name = list.Name
            };
        }

        /// <inheritdoc />
        public async Task Delete(Guid id) {
            var list = await DbContext.DistributionLists.FindAsync(id);
            if (list is null) {
                throw MessageExceptions.MessageTypeNotFound(id);
            }
            var associatedCampaigns = await DbContext.Campaigns.Where(x => x.DistributionListId == list.Id).Select(x => x.Title).ToArrayAsync();
            if (associatedCampaigns.Any()) {
                throw MessageExceptions.DistributionListAssociatedWithCampaigns(list.Name, associatedCampaigns);
            }
            DbContext.DistributionLists.Remove(list);
            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<DistributionList> GetById(Guid id) {
            var list = await DbContext.DistributionLists.FindAsync(id);
            if (list is null) {
                return default;
            }
            return new DistributionList {
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
                Id = list.Id,
                Name = list.Name
            };
        }

        /// <inheritdoc />
        public async Task<DistributionList> GetByName(string name) {
            var list = await DbContext.DistributionLists.SingleOrDefaultAsync(x => x.Name == name);
            if (list is null) {
                return default;
            }
            return new DistributionList {
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
                Id = list.Id,
                Name = list.Name
            };
        }

        /// <inheritdoc />
        public Task<ResultSet<DistributionList>> GetList(ListOptions options) {
            var query = DbContext
                .DistributionLists
                .AsNoTracking()
                .Select(list => new DistributionList {
                    CreatedAt = list.CreatedAt,
                    CreatedBy = list.CreatedBy,
                    Id = list.Id,
                    Name = list.Name
                });
            if (!string.IsNullOrWhiteSpace(options.Search)) {
                query = query.Where(x => x.Name.ToLower().Contains(options.Search.ToLower()));
            }
            return query.ToResultSetAsync(options);
        }

        /// <inheritdoc />
        public async Task Update(Guid id, UpdateDistributionListRequest request) {
            var list = await DbContext.DistributionLists.FindAsync(id);
            if (list is null) {
                throw MessageExceptions.MessageTypeNotFound(id);
            }
            list.Name = request.Name;
            await DbContext.SaveChangesAsync();
        }
    }
}
