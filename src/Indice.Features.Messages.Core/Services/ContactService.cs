using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

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
    public async Task<ContactsImportResult> BulkAddToDistributionList(Guid id, IEnumerable<CreateDistributionListContactRequest> requestList) {
        var list = await DbContext.DistributionLists.FindAsync(id);
        if (list is null) {
            throw MessageExceptions.DistributionListNotFound(id);
        }
        var result = new ContactsImportResult();

        var existingContactsInDistributionList = await DbContext.ContactDistributionLists
            .Where(x => x.DistributionListId == id && !string.IsNullOrWhiteSpace(x.Contact.Email))
            .Select(x => x.Contact)
            .ToListAsync();

        foreach (var request in requestList) {
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
    public async Task<Contact?> GetById(Guid id, bool expandPreferences = false) {
        var contact = await DbContext.Contacts.FindAsync(id);
        if (contact is null) {
            return default;
        }
        var result = Mapper.ToContact(contact);

        if (expandPreferences && !string.IsNullOrWhiteSpace(contact.RecipientId)) { 
            result.Preference = (from preference in DbContext.ContactPreferences
                                 where preference.RecipientId == contact.RecipientId
                                 select new ContactPreference {
                    Locale = preference.Locale,
                    ConsentCommercial = preference.ConsentCommercial,
                    ConsentCommercialDate = preference.ConsentCommercialDate,
                    Communication = DbContext.MessageTypes
                    .Select(mt => new ContactCommunicationOption {
                        MessageTypeAlias = new GuidOrAlias(mt.Alias ?? mt.Id.ToString()),
                        Channels = ContactChannelOption.FromKindFlags(preference.CommunicationOptions.Where(x => x.MessageTypeId == mt.Id).Select(x => x.Channels).FirstOrDefault()),
                    })
                    .ToList()}).FirstOrDefault() ?? 
                new ContactPreference() { 
                  Communication = DbContext.MessageTypes
                    .Select(mt => new ContactCommunicationOption {
                        MessageTypeAlias = new GuidOrAlias(mt.Alias ?? mt.Id.ToString()),
                        Channels = ContactChannelOption.FromKindFlags(ContactChannelKind.Any),
                    })
                    .ToList()
                };

        }

        return result;
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
            } else if (Guid.TryParse(searchTerm, out var _)) {
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
    public async Task<Contact?> GetByRecipientId(string? recipientId) {
        if (string.IsNullOrWhiteSpace(recipientId))
            return null;
        var query = DbContext.Contacts
                    .Where(contact => contact.RecipientId == recipientId)
                    .GroupJoin(
                        DbContext.ContactPreferences,
                        contact => contact.RecipientId,
                        rp => rp.RecipientId,
                        (contact, rps) => new { contact, rps }
                    )
                    .SelectMany(
                        x => x.rps.DefaultIfEmpty(),
                        (x, rp) => new Contact{
                            Id = x.contact.Id,
                            RecipientId = x.contact.RecipientId,
                            Email = x.contact.Email,
                            FirstName = x.contact.FirstName,
                            LastName = x.contact.LastName,
                            FullName = x.contact.FullName,
                            PhoneNumber = x.contact.PhoneNumber,
                            Salutation = x.contact.Salutation,
                            UpdatedAt = x.contact.UpdatedAt,
                            Preference = rp == null ? new ContactPreference() : new ContactPreference {
                                Locale = rp.Locale,
                                ConsentCommercial = rp.ConsentCommercial,
                                ConsentCommercialDate = rp.ConsentCommercialDate,
                                Communication = DbContext.ContactCommunicationOptions
                                    .Where(rcp => rcp.ContactPreferenceId == rp.Id)
                                    .Join(
                                        DbContext.MessageTypes,
                                        rcp => rcp.MessageTypeId,
                                        mt => mt.Id,
                                        (rcp, mt) => new { rcp, mt }
                                    )
                                    .Select(x => new ContactCommunicationOption {
                                        MessageTypeAlias = new GuidOrAlias(x.mt.Alias ?? x.mt.Id.ToString()),
                                        Channels = ContactChannelOption.FromKindFlags(x.rcp.Channels),
                                    })
                                    .ToList()
                            }
                        });
        return await query.SingleOrDefaultAsync();
    }
}
