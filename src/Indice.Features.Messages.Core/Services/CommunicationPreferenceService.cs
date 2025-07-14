using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Indice.Features.Messages.Core.Services;
internal class CommunicationPreferenceService : ICommunicationPreferenceService
{
    private readonly CampaignsDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="ContactService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CommunicationPreferenceService(CampaignsDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }


    public async Task<CommunicationPreference> GetPreferences(string recipientId) {
        var messageTypes = _dbContext.MessageTypes.AsNoTracking().Where(x=>x.Classification == MessageTypeClassification.Commercial);


        var result = await _dbContext.CommunicationPreferences
                            .Include(x => x.MessageTypeCommunicationPreferences)
                            .LeftJoin(messageTypes, cp => cp.MessageTypeCommunicationPreferences.Select(m => m.Type.Id),
                            mt => mt.Id, (cp, mt) => new {
                                cp.RecipientId,
                                cp.Locale,
                                MessageTypeCommunicationPreferences = cp.MessageTypeCommunicationPreferences
                                    .Where(m => m.Type.Id == mt.Id)
                                    .Select(m => new {
                                        m.CommunicationPreferences,
                                        Type = new {
                                            m.Type.Id,
                                            m.Type.Name,
                                            m.Type.Alias,
                                            m.Type.Classification
                                        }
                                    }).ToList()
                            })
                     .Where(x => x.RecipientId == recipientId)
                     .AsNoTracking();
        if (result is not null) {
            return result;
        }

        

        return new CommunicationPreference {
            RecipientId = recipientId,
            Locale = "en-US", // Default locale if not found
            MessageTypeCommunicationPreferences = new List<CommunicationMessageTypePreference>()
        };
    }

    public Task Update(string recipientId, UpdateCommunicationPreferenceRequest request) {
        throw new NotImplementedException();
    }
}
