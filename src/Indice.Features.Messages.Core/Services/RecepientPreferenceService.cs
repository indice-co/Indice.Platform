using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <inheritdoc/>
public class RecepientPreferenceService : IRecepientPreferenceService
{
    private readonly CampaignsDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="RecepientPreferenceService"/>.</summary>
    /// <param name="dbContext">The <see cref="DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RecepientPreferenceService(CampaignsDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<ContactPreference> GetPreferences(string recipientId) {
        var messageTypes = await _dbContext.MessageTypes.AsNoTracking().ToListAsync();
        var recipientPreferences = await _dbContext.ContactPreferences
                                            .Include(x => x.CommunicationOptions)
                                            .ThenInclude(up => up.MessageType)
                                            .AsNoTracking()
                                            .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        if (recipientPreferences == null) {
            return new ContactPreference {
                Locale = "en",
                Communication = messageTypes.Select(x =>
                new ContactCommunicationOption() {
                    MessageTypeAlias = new GuidOrAlias(x.Alias ?? x.Id.ToString()),
                    MessageTypeDisplayName = x.Name
                }).ToList(),
            };
        }
        //remove deleted
        recipientPreferences.CommunicationOptions.RemoveAll(x => !messageTypes.Any(mt => mt.Id == x.MessageTypeId));
        //add new types
        var missing = messageTypes.Where(x => !recipientPreferences.CommunicationOptions.Any(mt => mt.MessageTypeId == x.Id)).Select(cmt =>
            new DbContactCommunicationOption() {
                CommunicationPreferences = ContactChannelKind.Any,
                MessageType = cmt,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        recipientPreferences.CommunicationOptions.AddRange(missing);

        return new ContactPreference {
            Locale = recipientPreferences.Locale,
            ConsentCommercial = recipientPreferences.ConsentCommercial,
            ConsentCommercialDate = recipientPreferences.ConsentCommercialDate,
            Communication = recipientPreferences.CommunicationOptions.Select(x => new ContactCommunicationOption() {
                MessageTypeAlias = new GuidOrAlias(x.MessageType.Alias ?? x.MessageTypeId.ToString()),
                MessageTypeDisplayName = x.MessageType.Name,
                Channels = ContactChannelOption.FromKindFlags(x.CommunicationPreferences)
            }).ToList(),
        };
    }

    /// <inheritdoc/>
    public async Task Update(string recipientId, UpdatPreferenceRequest request) {
        var recipientPreferences = await _dbContext.ContactPreferences
                                           .Include(x => x.CommunicationOptions)
                                           .ThenInclude(up => up.MessageType)
                                           .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        var messageTypes = await _dbContext.MessageTypes
                                     .AsNoTracking()
                                     .ToListAsync();
        if (recipientPreferences == null) {
            recipientPreferences = new DbContactPreference() {
                RecipientId = recipientId,
                Locale = request.Locale,
                CommunicationOptions = messageTypes.Select(x =>
                    new DbContactCommunicationOption() {
                        MessageTypeId = x.Id,
                        CommunicationPreferences = request.CommunicationPreferences.FirstOrDefault(mt => mt.Alias == x.Alias)?.Channels.ToFlags() ?? ContactChannelKind.Any,
                        UpdatedAt = DateTimeOffset.UtcNow
                    }).ToList()
            };

            await _dbContext.ContactPreferences.AddAsync(recipientPreferences);
            await _dbContext.SaveChangesAsync();
            return;
        }

        recipientPreferences.Locale = request.Locale;
        //remove deleted
        recipientPreferences.CommunicationOptions.RemoveAll(x => !messageTypes.Any(mt => mt.Id == x.MessageTypeId));
        //update existing
        recipientPreferences.CommunicationOptions.ForEach(x => x.CommunicationPreferences = request.CommunicationPreferences.FirstOrDefault(mt => mt.Alias == x.MessageType.Alias)?.Channels.ToFlags() ?? ContactChannelKind.Any);
        //add new types
        var missing = messageTypes.Where(x => !recipientPreferences.CommunicationOptions.Any(mt => mt.MessageTypeId == x.Id)).Select(cmt =>
            new DbContactCommunicationOption() {
                CommunicationPreferenceId = recipientPreferences.Id,
                CommunicationPreferences = ContactChannelKind.Any,
                MessageType = cmt,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        recipientPreferences.CommunicationOptions.AddRange(missing);
        await _dbContext.SaveChangesAsync();
    }

    ///<inheritdoc/>
    public async Task UpdateContactPreferences(string recipientId, ContactPreference preference) {
        var recipientPreferences = await _dbContext.ContactPreferences
                                             .Include(x => x.CommunicationOptions)
                                             .ThenInclude(up => up.MessageType)
                                             .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        if (recipientPreferences == null) {
            var messageTypes = await _dbContext.MessageTypes
                                     .AsNoTracking()
                                     .ToListAsync();
            recipientPreferences = new DbContactPreference() {
                RecipientId = recipientId,
                Locale = preference.Locale,
                ConsentCommercial = preference.ConsentCommercial,
                ConsentCommercialDate = preference.ConsentCommercialDate,
                UpdatedAt = DateTimeOffset.UtcNow,
                CommunicationOptions = messageTypes.Select(x =>
                    new DbContactCommunicationOption() {
                        MessageTypeId = x.Id,
                        CommunicationPreferences = ContactChannelKind.Any,
                        UpdatedAt = DateTimeOffset.UtcNow
                    }).ToList()
            };

            await _dbContext.ContactPreferences.AddAsync(recipientPreferences);
            await _dbContext.SaveChangesAsync();
            return;
        }

        recipientPreferences.Locale = preference.Locale;
        recipientPreferences.ConsentCommercial = preference.ConsentCommercial;
        recipientPreferences.ConsentCommercialDate = preference.ConsentCommercialDate;
        await _dbContext.SaveChangesAsync();
    }
}
