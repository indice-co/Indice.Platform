using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
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
    public async Task<RecepientPreference> GetPreferences(string recipientId) {
        var messageTypes = await _dbContext.MessageTypes.AsNoTracking().ToListAsync();
        var recipientPreferences = await _dbContext.RecipientPreferences
                                            .Include(x => x.RecepientCommunicationPreferences)
                                            .ThenInclude(up => up.MessageType)
                                            .AsNoTracking()
                                            .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        if (recipientPreferences == null) {
            return new RecepientPreference {
                Locale = "en",
                CommunicationPreferences = messageTypes.Select(x =>
                new RecepientPreferenceCommunication() {
                    Alias = x.Alias,
                    Name = x.Name,
                    Channels = [ContactChannelKind.Any]
                }).ToList(),
            };
        }
        //remove deleted
        recipientPreferences.RecepientCommunicationPreferences.RemoveAll(x => !messageTypes.Any(mt => mt.Id == x.TypeId));
        //add new types
        var missing = messageTypes.Where(x => !recipientPreferences.RecepientCommunicationPreferences.Any(mt => mt.TypeId == x.Id)).Select(cmt =>
            new DbRecipientCommunicationPreference() {
                CommunicationPreferences = ContactChannelKind.Any,
                MessageType = cmt
            });
        recipientPreferences.RecepientCommunicationPreferences.AddRange(missing);

        return new RecepientPreference {
            Locale = recipientPreferences.Locale,
            CommunicationPreferences = recipientPreferences.RecepientCommunicationPreferences.Select(x => new RecepientPreferenceCommunication() {
                Alias = x.MessageType.Alias,
                Name = x.MessageType.Name,
                Channels = x.CommunicationPreferences.ToList()
            }).ToList(),
        };
    }

    /// <inheritdoc/>
    public async Task Update(string recipientId, UpdatPreferenceRequest request) {
        var recipientPreferences = await _dbContext.RecipientPreferences
                                           .Include(x => x.RecepientCommunicationPreferences)
                                           .ThenInclude(up => up.MessageType)
                                           .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        var messageTypes = await _dbContext.MessageTypes
                                     .AsNoTracking()
                                     .ToListAsync();
        if (recipientPreferences == null) {
            recipientPreferences = new DbRecipientPreference() {
                RecipientId = recipientId,
                Locale = request.Locale,
                RecepientCommunicationPreferences = messageTypes.Select(x =>
                    new DbRecipientCommunicationPreference() {
                        TypeId = x.Id,
                        CommunicationPreferences = request.CommunicationPreferences.FirstOrDefault(mt => mt.Alias == x.Alias)?.Channels.ToFlags() ?? ContactChannelKind.Any,
                    }).ToList()
            };

            await _dbContext.RecipientPreferences.AddAsync(recipientPreferences);
            await _dbContext.SaveChangesAsync();
            return;
        }

        recipientPreferences.Locale = request.Locale;
        //remove deleted
        recipientPreferences.RecepientCommunicationPreferences.RemoveAll(x => !messageTypes.Any(mt => mt.Id == x.TypeId));
        //update existing
        recipientPreferences.RecepientCommunicationPreferences.ForEach(x => x.CommunicationPreferences = request.CommunicationPreferences.FirstOrDefault(mt => mt.Alias == x.MessageType.Alias)?.Channels.ToFlags() ?? ContactChannelKind.Any);
        //add new types
        var missing = messageTypes.Where(x => !recipientPreferences.RecepientCommunicationPreferences.Any(mt => mt.TypeId == x.Id)).Select(cmt =>
            new DbRecipientCommunicationPreference() {
                CommunicationPreferenceId = recipientPreferences.Id,
                CommunicationPreferences = ContactChannelKind.Any,
                MessageType = cmt
            });
        recipientPreferences.RecepientCommunicationPreferences.AddRange(missing);
        await _dbContext.SaveChangesAsync();
    }

    ///<inheritdoc/>
    public async Task UpdateContactPreferences(string recipientId, RecepientPreference preference) {
        var recipientPreferences = await _dbContext.RecipientPreferences
                                             .Include(x => x.RecepientCommunicationPreferences)
                                             .ThenInclude(up => up.MessageType)
                                             .SingleOrDefaultAsync(x => x.RecipientId == recipientId);
        if (recipientPreferences == null) {
            var messageTypes = await _dbContext.MessageTypes
                                     .AsNoTracking()
                                     .ToListAsync();
            recipientPreferences = new DbRecipientPreference() {
                RecipientId = recipientId,
                Locale = preference.Locale,
                ConsentCommercial = preference.ConsentCommercial,
                ConsentCommercialDate = preference.ConsentCommercialDate,
                RecepientCommunicationPreferences = messageTypes.Select(x =>
                    new DbRecipientCommunicationPreference() {
                        TypeId = x.Id,
                        CommunicationPreferences = ContactChannelKind.Any
                    }).ToList()
            };

            await _dbContext.RecipientPreferences.AddAsync(recipientPreferences);
            await _dbContext.SaveChangesAsync();
            return;
        }

        recipientPreferences.Locale = preference.Locale;
        recipientPreferences.ConsentCommercial = preference.ConsentCommercial;
        recipientPreferences.ConsentCommercialDate = preference.ConsentCommercialDate;
        await _dbContext.SaveChangesAsync();
    }
}
