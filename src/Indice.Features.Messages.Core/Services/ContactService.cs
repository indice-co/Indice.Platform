using System.Data.Common;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IContactService"/> for Entity Framework Core.</summary>
public class ContactService : IContactService
{
    /// <summary>Creates a new instance of <see cref="ContactService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <param name="eventDispatcherFactory">The <see cref="IEventDispatcherFactory"/> for dispatching events.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ContactService(CampaignsDbContext dbContext, IEventDispatcherFactory eventDispatcherFactory) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        EventDispatcherFactory = eventDispatcherFactory ?? throw new ArgumentNullException(nameof(eventDispatcherFactory));
    }
    private IEventDispatcherFactory EventDispatcherFactory { get; }
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

        if (!string.IsNullOrWhiteSpace(request.RecipientId)) {
            var knownContact = await DbContext.Contacts
                               .OrderByDescending(x => x.UpdatedAt)
                               .Where(x => x.RecipientId == request.RecipientId)
                               .FirstOrDefaultAsync();
            if (knownContact is not null) {
                knownContact.Email = request.Email;
                knownContact.FirstName = request.FirstName;
                knownContact.FullName = request.FullName;
                knownContact.LastName = request.LastName;
                knownContact.PhoneNumber = request.PhoneNumber;
                knownContact.Salutation = request.Salutation;
                knownContact.UpdatedAt = DateTimeOffset.UtcNow;
                knownContact.Resolved = request.Resolved || knownContact.Resolved.GetValueOrDefault();
                if (request.Resolved) {
                    knownContact.LastResolutionDate = request.LastResolutionDate ?? knownContact.LastResolutionDate;
                }
                await DbContext.SaveChangesAsync();
                return Mapper.ToContact(knownContact);
            }
        }
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
            result.Preference = await GetContactPreference(contact.RecipientId);

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
        if (filter?.Anonymous == true) {
            query = query.Where(x => x.RecipientId == null || x.Resolved == false);
        }
        if (filter?.Anonymous == false) {
            query = query.Where(x => x.RecipientId != null && x.Resolved == true);
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
        contact.Resolved = contact.Resolved == true || request.Resolved == true;
        if (request.Resolved == true) {
            contact.LastResolutionDate = request.LastResolutionDate ?? DateTimeOffset.UtcNow;
        }
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
            ContactId = contact.Id,
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
                        (x, rp) => new Contact {
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
                                DefaultChannels = rp.DefaultChannels == null ? null : ContactChannelOption.FromKindFlags(rp.DefaultChannels.Value),
                                Communication = DbContext.ContactCommunicationOptions
                                    .Where(rcp => rcp.ContactPreferenceId == rp.Id)
                                    .Join(
                                        DbContext.MessageTypes,
                                        rcp => rcp.MessageTypeId,
                                        mt => mt.Id,
                                        (rcp, mt) => new { rcp, mt }
                                    )
                                    .Select(x => new ContactCommunicationOption {
                                        MessageType = new MessageType {
                                            Id = x.mt.Id,
                                            Alias = new GuidOrAlias(x.mt.Alias ?? x.mt.Id.ToString()),
                                            Classification = x.mt.Classification,
                                            Name = x.mt.Name
                                        },
                                        Channels = ContactChannelOption.FromKindFlags(x.rcp.Channels),
                                    })
                                    .ToList()
                            }
                        });
        try {
            return await query.SingleOrDefaultAsync();
        } catch (InvalidOperationException) {
            // More than one matching record was found
            var duplicateContacts = await query.ToListAsync();
            var primaryRecord = duplicateContacts.OrderByDescending(r => r.UpdatedAt).First();
            var eventDispatcher = EventDispatcherFactory.Create(Core.KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                new MergeContactsEvent() {
                    PrimaryContactId = primaryRecord.Id!.Value,
                    DuplicateContactsIds = [.. duplicateContacts.Where(r => r.Id != primaryRecord.Id).Select(r => r.Id!.Value)]
                },
                builder => builder.WrapInEnvelope().WithQueueName(EventNames.MergeContacts)
            );
            return primaryRecord;
        }
    }



    /// <inheritdoc/>
    public async Task<ContactPreference> GetContactPreference(string recipientId) {
        var messageTypes = await DbContext.MessageTypes.AsNoTracking().ToListAsync();
        var recipientPreferences = await DbContext.ContactPreferences
                                            .Include(x => x.CommunicationOptions)
                                            .ThenInclude(up => up.MessageType)
                                            .AsNoTracking()
                                            .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        if (recipientPreferences == null) {
            return new ContactPreference {
                Locale = "en",
                Communication = messageTypes.Select(x =>
                new ContactCommunicationOption() {
                    MessageType = new MessageType {
                        Id = x.Id,
                        Alias = new GuidOrAlias(x.Alias ?? x.Id.ToString()),
                        Classification = x.Classification,
                        Name = x.Name
                    },
                }).ToList(),
            };
        }
        //remove deleted
        recipientPreferences.CommunicationOptions.RemoveAll(x => !messageTypes.Any(mt => mt.Id == x.MessageTypeId));
        //add new types
        var missing = messageTypes.Where(x => !recipientPreferences.CommunicationOptions.Any(mt => mt.MessageTypeId == x.Id)).Select(cmt =>
            new DbContactCommunicationOption() {
                Channels = ContactChannelOption.ToContactChannelKind(ContactChannelOption.FromKindFlags(ContactChannelKind.Any)),
                MessageType = cmt,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        recipientPreferences.CommunicationOptions.AddRange(missing);

        return new ContactPreference {
            Locale = recipientPreferences.Locale,
            ConsentCommercial = recipientPreferences.ConsentCommercial,
            ConsentCommercialDate = recipientPreferences.ConsentCommercialDate,
            DefaultChannels = recipientPreferences.DefaultChannels != null ? ContactChannelOption.FromKindFlags(recipientPreferences.DefaultChannels.Value) : null,
            Communication = recipientPreferences.CommunicationOptions.Select(x => new ContactCommunicationOption() {
                MessageType = new MessageType {
                    Id = x.MessageType.Id,
                    Alias = new GuidOrAlias(x.MessageType.Alias ?? x.MessageType.Id.ToString()),
                    Classification = x.MessageType.Classification,
                    Name = x.MessageType.Name
                },
                Channels = ContactChannelOption.FromKindFlags(x.Channels)
            }).ToList()
        };
    }

    /// <inheritdoc/>
    public async Task UpdatePreference(string recipientId, UpdatePreferenceRequest request) {
        var recipientPreferences = await DbContext.ContactPreferences
                                           .Include(x => x.CommunicationOptions)
                                           .ThenInclude(up => up.MessageType)
                                           .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        var messageTypes = await DbContext.MessageTypes
                                     .AsNoTracking()
                                     .ToListAsync();
        if (recipientPreferences == null) {
            recipientPreferences = new DbContactPreference() {
                RecipientId = recipientId,
                Locale = request.Locale,
                CommunicationOptions = messageTypes.Select(x =>
                    new DbContactCommunicationOption() {
                        MessageTypeId = x.Id,
                        Channels = ContactChannelOption.ToContactChannelKind(request.Communication.FirstOrDefault(mt => mt.MessageTypeAlias == x.Alias || mt.MessageTypeAlias == x.Id)?.Channels ?? ContactChannelOption.FromKindFlags(ContactChannelKind.Any)),
                        UpdatedAt = DateTimeOffset.UtcNow
                    }).ToList()
            };
            try {
                await DbContext.ContactPreferences.AddAsync(recipientPreferences);
                await DbContext.SaveChangesAsync();
                return;
            } catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex)) {
                DbContext.ChangeTracker.Clear();
                recipientPreferences = await DbContext.ContactPreferences
                                           .Include(x => x.CommunicationOptions)
                                           .ThenInclude(up => up.MessageType)
                                           .SingleAsync(x => x.RecipientId == recipientId);
            }
        }

        recipientPreferences.Locale = request.Locale;
        recipientPreferences.ConsentCommercial = request.ConsentCommercial;
        recipientPreferences.ConsentCommercialDate = request.ConsentCommercialDate;
        recipientPreferences.DefaultChannels = request.DefaultChannels != null ? ContactChannelOption.ToContactChannelKind(request.DefaultChannels!) : null;
        //remove deleted
        recipientPreferences.CommunicationOptions.RemoveAll(x => !messageTypes.Any(mt => mt.Id == x.MessageTypeId));
        //update existing
        recipientPreferences.CommunicationOptions.ForEach(x => x.Channels = ContactChannelOption.ToContactChannelKind(request.Communication.FirstOrDefault(mt => mt.MessageTypeAlias == x.MessageType.Alias || mt.MessageTypeAlias == x.MessageType.Id)?.Channels ?? ContactChannelOption.FromKindFlags(ContactChannelKind.Any)));
        //add new types
        var missing = messageTypes.Where(x => !recipientPreferences.CommunicationOptions.Any(mt => mt.MessageTypeId == x.Id)).Select(cmt =>
            new DbContactCommunicationOption() {
                ContactPreferenceId = recipientPreferences.Id,
                Channels = ContactChannelKind.Any,
                MessageType = cmt,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        recipientPreferences.CommunicationOptions.AddRange(missing);
        await DbContext.SaveChangesAsync();
    }

    ///<inheritdoc/>
    public async Task UpdateContactPreferences(string recipientId, ContactPreference preference) {
        var recipientPreferences = await DbContext.ContactPreferences
                                             .Include(x => x.CommunicationOptions)
                                             .ThenInclude(up => up.MessageType)
                                             .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        if (recipientPreferences == null) {
            recipientPreferences = new DbContactPreference() {
                RecipientId = recipientId,
                Locale = preference.Locale,
                ConsentCommercial = preference.ConsentCommercial,
                ConsentCommercialDate = preference.ConsentCommercialDate,
                DefaultChannels = preference.DefaultChannels != null ? ContactChannelOption.ToContactChannelKind(preference.DefaultChannels) : null,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            try {
                await DbContext.ContactPreferences.AddAsync(recipientPreferences);
                await DbContext.SaveChangesAsync();
                return;
            } catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex)) {
                DbContext.ChangeTracker.Clear();
                recipientPreferences = await DbContext.ContactPreferences
                                             .Include(x => x.CommunicationOptions)
                                             .ThenInclude(up => up.MessageType)
                                             .SingleAsync(x => x.RecipientId == recipientId);
            }
        }
        recipientPreferences.Locale = preference.Locale;
        recipientPreferences.ConsentCommercial = preference.ConsentCommercial;
        recipientPreferences.ConsentCommercialDate = preference.ConsentCommercialDate;
        recipientPreferences.DefaultChannels = preference.DefaultChannels != null ? ContactChannelOption.ToContactChannelKind(preference.DefaultChannels) : null;
        await DbContext.SaveChangesAsync();
    }

    private bool IsDuplicateKeyViolation(DbUpdateException ex) {
        return ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key row");
    }

    ///<inheritdoc/>
    public async Task<ResultSet<Contact>> GetDuplicates(Contact mainContact, ListOptions options) {
        var duplicateContactsQuery = DbContext.Contacts
            .Where(x => (x.RecipientId == mainContact.RecipientId || x.Email!.ToLower() == mainContact.Email!.ToLower()) && x.Id != mainContact.Id)
            .Select(x => Mapper.ToContact(x));
        if (!string.IsNullOrWhiteSpace(options.Search)) {
            duplicateContactsQuery = duplicateContactsQuery.Where(x => x.FullName!.Contains(options.Search));
        }
        return await duplicateContactsQuery.ToResultSetAsync(options);
    }

    ///<inheritdoc/>
    public async Task MergeContacts(Contact mainContact, List<Guid> duplicateContactsIds) {
        var existingDuplicateContactIds = await DbContext.Contacts.Where(x => duplicateContactsIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        if (existingDuplicateContactIds.Count == 0) {
            return;
        }
        await MergeDistributionListContacts(mainContact, existingDuplicateContactIds);
        await UpdateContactInMessageEvents(mainContact, existingDuplicateContactIds);
        await UpdateContactInMessages(mainContact, existingDuplicateContactIds);
        await DbContext.Contacts.Where(x => existingDuplicateContactIds.Contains(x.Id)).ExecuteDeleteAsync();
        await DbContext.SaveChangesAsync();
    }
    private async Task MergeDistributionListContacts(Contact mainContact, List<Guid> duplicateContactIds) {
        // The IDs of the distribution lists where the main contact does not exist, but at least one of the duplicate contacts does
        var affectedDistributionListIds = await DbContext.DistributionLists
            .Where(x => !x.ContactDistributionLists.Any(c => c.ContactId == mainContact.Id) &&
                         x.ContactDistributionLists.Any(c => duplicateContactIds.Contains(c.ContactId)))
            .Select(x => x.Id).Distinct().ToListAsync();

        await DbContext.ContactDistributionLists.Where(x => duplicateContactIds.Contains(x.ContactId)).ExecuteDeleteAsync();
        DbContext.ContactDistributionLists.AddRange(affectedDistributionListIds.Select(id => new DbDistributionListContact {
            DistributionListId = id,
            ContactId = mainContact.Id!.Value,
        }));
        await DbContext.SaveChangesAsync();
    }

    private async Task UpdateContactInMessages(Contact mainContact, List<Guid> duplicateContactIds) {
        await DbContext.Messages
            .Where(x => duplicateContactIds
            .Contains(x.ContactId!.Value))
            .ExecuteUpdateAsync(setters =>
            setters.SetProperty(x => x.ContactId, mainContact.Id)
            .SetProperty(x => x.RecipientId, mainContact.RecipientId));
    }

    private async Task UpdateContactInMessageEvents(Contact mainContact, List<Guid> duplicateContactIds) {
        await DbContext.MessageEvents
        .Where(x => duplicateContactIds.Contains(x.ContactId))
        .ExecuteUpdateAsync(setter => setter.SetProperty(x => x.ContactId, mainContact.Id));
    }
}
