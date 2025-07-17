using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <inheritdoc/>
public class CommunicationPreferenceService : ICommunicationPreferenceService
{
    private readonly CampaignsDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="ContactService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CommunicationPreferenceService(CampaignsDbContext dbContext) {
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
                    CommunicationPreferences = ContactChannelKind.Any
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
                CommunicationPreferences = x.CommunicationPreferences
            }).ToList(),
        };
    }

    /// <inheritdoc/>
    public async Task Update(string recipientId, UpdateCommunicationPreferenceRequest request) {
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
                        CommunicationPreferences = request.CommunicationPreferencesPerMessageType.FirstOrDefault(mt => mt.TypeId == x.Alias)?.CommunicationPreferences ?? ContactChannelKind.Any,
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
        recipientPreferences.RecepientCommunicationPreferences.ForEach(x => x.CommunicationPreferences = request.CommunicationPreferencesPerMessageType.FirstOrDefault(mt => mt.TypeId == x.MessageType.Alias)?.CommunicationPreferences ?? ContactChannelKind.Any);
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
}
