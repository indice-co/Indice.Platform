using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IContactService"/> for Entity Framework Core.</summary>
public class ContactService : IContactService
{
    /// <summary>Creates a new instance of <see cref="ContactService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ContactService(CampaignsDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private CampaignsDbContext DbContext { get; }

    /// <inheritdoc />
    public async Task AddToDistributionList(Guid id, CreateDistributionListContactRequest request) {
        var list = await DbContext.DistributionLists.FindAsync(id);
        if (list is null) {
            throw MessageExceptions.DistributionListNotFound(id);
        }
        list.UpdatedAt = DateTimeOffset.UtcNow;
        DbContact? contact;
        if (request.ContactId.HasValue) {
            contact = await DbContext.Contacts.SingleOrDefaultAsync(x => x.Id == request.ContactId);
            if (contact is null) {
                throw MessageExceptions.ContactNotFound(id);
            }
            await AddContactToDistributionList(contact, list, request);
            return;
        }
        if (!string.IsNullOrWhiteSpace(request.RecipientId)) {
            contact = await DbContext.Contacts.FirstOrDefaultAsync(x => x.RecipientId == request.RecipientId);
            if (contact is not null) {
                await AddContactToDistributionList(contact, list, request);
                return;
            }
        }

        contact = Mapper.ToDbContact(request);
        contact.DistributionListContacts.Add(new DbDistributionListContact {
            ContactId = Guid.NewGuid(),
            DistributionListId = list.Id
        });
        DbContext.Contacts.Add(contact);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<ContactsImportResult> BulkAddToDistributionList(Guid id, IEnumerable<CreateDistributionListContactRequest> requests) {
        var list = await DbContext.DistributionLists.FindAsync(id);
        if (list is null) {
            throw MessageExceptions.DistributionListNotFound(id);
        }
        var result = new ContactsImportResult();

        var existingContactsInDistributionList = await DbContext.ContactDistributionLists
            .Where(x => x.DistributionListId == id && !string.IsNullOrWhiteSpace(x.Contact.Email))
            .Select(x => x.Contact)
            .ToListAsync();

        foreach (var request in requests) {
            try {
                DbContact? contact;

                if (!string.IsNullOrWhiteSpace(request.RecipientId)) {
                    contact = await DbContext.Contacts.FirstOrDefaultAsync(x => x.RecipientId == request.RecipientId);

                    if (contact is not null) {
                        await AddContactToDistributionListIfNotExists(contact, list);
                        result.ContactsUpdated++;
                        continue;
                    }

                    CreateAndAddContactToDistributionList(request, list);
                    result.ContactsAdded++;
                } else {
                    var existingContact = existingContactsInDistributionList
                        .FirstOrDefault(a => a.Email!.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

                    if (existingContact is not null) {
                        if (string.IsNullOrWhiteSpace(existingContact.RecipientId)) {
                            existingContact.MapFromCreateDistributionListContactRequest(request);
                            result.ContactsUpdated++;
                        }
                        continue;
                    }

                    CreateAndAddContactToDistributionList(request, list);
                    result.ContactsAdded++;
                }
            } catch (DbUpdateException dbEx) {
                result.Errors.Add($"Database error processing contact with Email '{request.Email}': {dbEx.Message}");
            } catch (ArgumentException argEx) {
                result.Errors.Add($"Invalid argument for contact with Email '{request.Email}': {argEx.Message}");
            } catch (Exception ex) {
                result.Errors.Add($"Unexpected error processing contact with Email '{request.Email}': {ex.Message}");
            }
        }

        await DbContext.SaveChangesAsync();
        return result;
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
        DbContext.Contacts.AddRange(contacts.Select(Mapper.ToDbContact));
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Contact?> GetById(Guid id) {
        var contact = await DbContext.Contacts.FindAsync(id);
        if (contact is null) {
            return default;
        }
        return Mapper.ToContact(contact);
    }

    /// <inheritdoc />
    public async Task<ResultSet<Contact>> GetList(ListOptions<ContactListFilter> options) {
        var query = DbContext.Contacts
                            .AsNoTracking();
        var filter = options.Filter;
        if (filter?.DistributionListId is not null) {
            query = query.Include(x => x.DistributionListContacts);
            query = query.Where(x => x.DistributionListContacts.Any(y => y.DistributionListId == filter.DistributionListId.Value));
        }
        if (filter?.Email is not null) {
            query = query.Where(x => x.Email!.ToLower() == filter.Email.ToLower());
        }
        if (filter?.PhoneNumber is not null) {
            query = query.Where(x => x.PhoneNumber!.ToLower() == filter.PhoneNumber.ToLower());
        }
        if (filter?.RecipientId is not null) {
            query = query.Where(x => x.RecipientId!.ToLower() == filter.RecipientId.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(options.Search)) {
            var searchTerm = options.Search.Trim().ToLowerInvariant();
            if (int.TryParse(searchTerm, out var number)) {
                query = query.Where(x => x.RecipientId!.Contains(number.ToString())
                                      || x.PhoneNumber!.Contains(number.ToString()));
            } else if (Guid.TryParse(searchTerm, out var guid)) {
                query = query.Where(x => x.RecipientId!.ToLower() == searchTerm);
            } else {
                query = query.Where(x => x.FirstName!.ToLower().Contains(searchTerm) ||
                                    x.LastName!.ToLower().Contains(searchTerm) ||
                                   (x.Email != null && x.Email.ToLower().Contains(searchTerm)) ||
                                   (x.PhoneNumber != null && x.PhoneNumber.ToLower().Contains(searchTerm)));
            }
        }

        return await query.Select(Mapper.ProjectToContact).ToResultSetAsync(options);
    }

    /// <inheritdoc />
    public async Task<Contact[]> GetByDistributionList(Guid id) {
        return await DbContext.Contacts
            .AsNoTracking()
            .Include(x => x.DistributionListContacts)
            .Where(x => x.DistributionListContacts.Any(y => y.DistributionListId == id))
            .Select(Mapper.ProjectToContact)
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task RemoveFromDistributionList(Guid id, Guid contactId) {
        var association = await DbContext.ContactDistributionLists.SingleOrDefaultAsync(x => x.ContactId == contactId && x.DistributionListId == id);
        if (association is null) {
            throw MessageExceptions.DistributionListContactAssociationNotFound(id, contactId);
        }
        DbContext.ContactDistributionLists.Remove(association);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task Update(Guid id, UpdateContactRequest request) {
        var contact = await DbContext.Contacts
                                    .FindAsync(id);
        if (contact is null) {
            throw MessageExceptions.ContactNotFound(id);
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

    private async Task AddContactToDistributionList(DbContact contact, DbDistributionList list, CreateDistributionListContactRequest request) {
        var associationExists = await DbContext.ContactDistributionLists.AnyAsync(x => x.ContactId == contact.Id && x.DistributionListId == list.Id);
        if (associationExists) {
            throw MessageExceptions.ContactAlreadyInDistributionList(list.Id, contact.Id);
        }
        contact.DistributionListContacts.Add(new DbDistributionListContact {
            ContactId = contact.Id,
            DistributionListId = list.Id
        });
        contact.MapFromCreateDistributionListContactRequest(request);
        await DbContext.SaveChangesAsync();
    }

    private async Task AddContactToDistributionListIfNotExists(DbContact contact, DbDistributionList list) {
        var associationExists = await DbContext.ContactDistributionLists.AnyAsync(x => x.ContactId == contact.Id && x.DistributionListId == list.Id);
        if (associationExists) {
            return;
        }
        contact.DistributionListContacts.Add(new DbDistributionListContact {
            ContactId = contact.Id,
            DistributionListId = list.Id
        });
    }

    private void CreateAndAddContactToDistributionList(CreateDistributionListContactRequest request, DbDistributionList list) {
        var contact = Mapper.ToDbContact(request);
        contact.DistributionListContacts.Add(new DbDistributionListContact {
            ContactId = Guid.NewGuid(),
            DistributionListId = list.Id
        });
        DbContext.Contacts.Add(contact);
    }
    /// <summary>Gets a contact by it's recipient id.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <returns></returns>
    public async Task<ContactPreferences?> GetByRecipientId(string? recipientId) {
        if (string.IsNullOrWhiteSpace(recipientId))
            return null;

        return await DbContext.Contacts
                    .Where(contact => contact.RecipientId!.ToLower() == recipientId.ToLower())
                    .Join(DbContext.RecipientPreferences,
                            contact => contact.RecipientId,
                            rp => rp.RecipientId,
                            (contact, rp) => new ContactPreferences() {
                                Id = contact.Id,
                                RecipientId = contact.RecipientId,
                                Email = contact.Email,
                                FirstName = contact.FirstName,
                                LastName = contact.LastName,
                                FullName = contact.FullName,
                                PhoneNumber = contact.PhoneNumber,
                                Salutation = contact.Salutation,
                                UpdatedAt = contact.UpdatedAt,
                                Preferences = rp == null ? new RecepientPreference() : new RecepientPreference() {
                                    Locale = rp.Locale,
                                    ConsentCommercial = rp.ConsentCommercial,
                                    ConsentCommercialDate = rp.ConsentCommercialDate,
                                    CommunicationPreferences = DbContext.RecipientCommunicationPreferences
                                                               .Where(rcp => rcp.CommunicationPreferenceId == rp.Id)
                                                               .Join(DbContext.MessageTypes,
                                                                    rcp => rcp.TypeId,
                                                                    mt => mt.Id,
                                                                    (rcp, mt) => new { rcp, mt })
                                                               .Select(x => new RecepientPreferenceCommunication() {
                                                                   Alias = x.mt.Alias,
                                                                   Channels = x.rcp.CommunicationPreferences.ToList()
                                                               }).ToList()
                                }
                            }).SingleOrDefaultAsync();
    }
}
