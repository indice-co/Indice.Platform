using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Exceptions;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class ContactService : IContactService
    {
        public ContactService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public CampaignsDbContext DbContext { get; }

        public async Task AddToDistributionList(CreateDistributionListContactRequest request, Guid id) {
            DbContact contact;
            var list = await DbContext.DistributionLists.FindAsync(id);
            if (list is null) {
                throw new CampaignException($"Distribution list with id '{request.Id.Value}' does not exist.", nameof(id));
            }
            if (request.Id.HasValue) {
                contact = await DbContext.Contacts.FindAsync(request.Id.Value);
                if (contact is null) {
                    throw new CampaignException($"Contact with id '{request.Id.Value}' does not exist.", nameof(request.Id));
                }
                contact.DistributionListId = list.Id;
                contact.Email = request.Email;
                contact.FirstName = request.FirstName;
                contact.FullName = request.FullName;
                contact.Id = request.Id.HasValue ? request.Id.Value : Guid.NewGuid();
                contact.LastName = request.LastName;
                contact.PhoneNumber = request.PhoneNumber;
                contact.RecipientId = request.RecipientId;
                contact.Salutation = request.Salutation;
                await DbContext.SaveChangesAsync();
                return;
            }
            contact = Mapper.ToDbContact(request);
            contact.DistributionListId = id;
            DbContext.Contacts.Add(contact);
            await DbContext.SaveChangesAsync();
        }

        public async Task<Contact> Create(CreateContactRequest request) {
            var contact = Mapper.ToDbContact(request);
            DbContext.Contacts.Add(contact);
            await DbContext.SaveChangesAsync();
            return Mapper.ToContact(contact);
        }

        public async Task<Contact> GetById(Guid id) {
            var contact = await DbContext.Contacts.FindAsync(id);
            if (contact is null) {
                return default;
            }
            return Mapper.ToContact(contact);
        }

        public async Task<ResultSet<Contact>> GetList(ListOptions options) {
            var query = DbContext
                .Contacts
                .AsNoTracking()
                .Select(Mapper.ProjectToContact);
            return await query.ToResultSetAsync(options);
        }
    }
}
