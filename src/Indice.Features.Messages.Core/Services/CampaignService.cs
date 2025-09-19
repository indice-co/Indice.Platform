using System.Collections.Generic;
using System.Text.Json;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Serialization;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="ICampaignService"/> for Entity Framework Core.</summary>
public class CampaignService : ICampaignService
{
    /// <summary>Creates a new instance of <see cref="CampaignService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <param name="campaignManagementOptions">Options used to configure the Campaigns management API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignService(
        CampaignsDbContext dbContext,
        IOptions<MessageManagementOptions> campaignManagementOptions
    ) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        CampaignManagementOptions = campaignManagementOptions?.Value ?? throw new ArgumentNullException(nameof(campaignManagementOptions));
    }

    private CampaignsDbContext DbContext { get; }
    private MessageManagementOptions CampaignManagementOptions { get; }

    /// <inheritdoc />
    public Task<ResultSet<Campaign>> GetList(ListOptions<CampaignListFilter> options) {
        var query = DbContext
                .Campaigns
                .Include(x => x.Type)
                .Include(x => x.DistributionList)
                .AsNoTracking();

        if (options.Filter?.ContactId.HasValue == true) {
            query = from o in query
                    join c in DbContext.ContactDistributionLists on o.DistributionListId equals c.DistributionListId
                    where c.ContactId == options.Filter.ContactId.Value
                    select o;
        }


        if (options.Filter?.MessageChannelKind?.Length > 0) {
            var kind = options.Filter.MessageChannelKind.ToFlags();
            query = query.Where(x => x.MessageChannelKind.HasFlag(kind));
        }

        var projectedQuery = query.Select(Mapper.ProjectToCampaign);

        if (!string.IsNullOrEmpty(options.Search) && options.Search.Length > 2) {
            var searchTerm = options.Search.Trim();
            projectedQuery = projectedQuery.Where(x => x.Title != null && x.Title.Contains(searchTerm));
        }
        if (options.Filter?.Published.HasValue == true) {
            projectedQuery = projectedQuery.Where(x => x.Published == options.Filter.Published.Value);
        }
        if (options.Filter?.TypeId?.Length > 0) {
            var types = options.Filter.TypeId.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => GuidOrAlias.Parse(x));
            var typeIds = types.Where(x => x.IsGuid).Select(x => x.Uuid).ToArray();
            var typeAliases = types.Where(x => !x.IsGuid).Select(x => x.Value).ToArray();
            projectedQuery = projectedQuery.Where(x => typeIds.Contains(x.Type!.Id) || typeAliases.Contains(x.Type.Alias));
        }
        return projectedQuery.ToResultSetAsync(options);
    }

    /// <inheritdoc />
    public async Task<CampaignDetails?> GetById(Guid campaignId) {
        var campaign = await DbContext
            .Campaigns
            .AsNoTracking()
            .Include(x => x.Attachment)
            .Include(x => x.Type)
            .Include(x => x.DistributionList)
            .Select(Mapper.ProjectToCampaignDetails)
            .SingleOrDefaultAsync(x => x.Id == campaignId);
        if (campaign is null) {
            return default;
        }
        if (campaign.Attachment is not null) {
            campaign.Attachment.PermaLink = $"{CampaignManagementOptions.PathPrefix.TrimEnd('/')}/{campaign.Attachment.PermaLink?.TrimStart('/')}";
        }
        return campaign;
    }

    /// <inheritdoc />
    public async Task<Campaign> Create(CreateCampaignRequest request) {
        var dbCampaign = Mapper.ToDbCampaign(request);
        DbContext.Campaigns.Add(dbCampaign);
        await DbContext.SaveChangesAsync();
        return Mapper.ToCampaign(dbCampaign);
    }

    /// <inheritdoc />
    public async Task Update(Guid campaignId, UpdateCampaignRequest request) {
        var campaign = await DbContext.Campaigns.SingleOrDefaultAsync(x => x.Id == campaignId);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(campaignId);
        }
        if (campaign.Published) {
            throw MessageExceptions.CampaignAlreadyPublished(campaignId);
        }
        campaign.ActionLink = request.ActionLink;
        campaign.MediaBaseHref = request.MediaBaseHref;
        campaign.ActivePeriod = request.ActivePeriod;
        campaign.Content = request.Content;
        campaign.MessageChannelKind = Enum.Parse<MessageChannelKind>(string.Join(',', request.Content.Select(x => x.Key)), ignoreCase: true);
        campaign.Title = request.Title;
        campaign.TypeId = request.TypeId;
        campaign.DistributionListId = request.RecipientListId;
        campaign.Data = request.Data;
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task Delete(Guid campaignId) {
        var campaign = await DbContext.Campaigns.FindAsync(campaignId);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(campaignId);
        }
        DbContext.Remove(campaign);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<CampaignStatistics?> GetStatistics(Guid campaignId) {
        var campaign = await DbContext.Campaigns.FindAsync(campaignId);
        if (campaign is null) {
            return default;
        }
        var callToActionCount = await DbContext.Hits.AsNoTracking().CountAsync(x => x.CampaignId == campaignId);
        var readCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && x.IsRead);
        var deletedCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && x.IsDeleted);
        int? notReadCount = null;
        if (!campaign.IsGlobal) {
            notReadCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && !x.IsRead);
        }
        var recepientsNumber = await DbContext.MessageEvents
                                .Where(m => m.CampaignId == campaignId)
                                .Select(m => m.ContactId)
                                .Distinct()
                                .CountAsync();
        var countPerChanel = await DbContext.MessageEvents.Where(x => x.CampaignId == campaignId && x.Type == MessageEventType.Sent.ToString()).GroupBy(m => m.Channel).ToDictionaryAsync(g => g.Key, g => g.Count());
        return new CampaignStatistics {
            CallToActionCount = callToActionCount,
            DeletedCount = deletedCount,
            LastUpdated = DateTime.UtcNow,
            NotReadCount = notReadCount,
            ReadCount = readCount,
            Title = campaign.Title,
            MessagesperChannel = countPerChanel,
            RecipientsCount = recepientsNumber
        };
    }

    /// <inheritdoc />
    public async Task<RecipientStats?> GetPerformance(Guid? campaignId = null) {
        /*
         
SELECT C.Id as CampaignId, 
	   C.Title as CampaignTitle, 
	   (select count(CT.ContactId) 
		from [cmp].[DistributionListContact] CT
		join [cmp].[DistributionList] D on D.Id = CT.DistributionListId
		where D.id = C.DistributionListId
	   ) as RecipientCount,
	   (SELECT COUNT(DISTINCT MessageId) from [cmp].[MessageEvent] WHERE CampaignId = C.Id AND [Type] = 'Sent' OR ([Channel] = 'Inbox' and [Type] = 'Created')) as [RecipientsReached]
from [cmp].[Campaign] C 
        */
        if (campaignId.HasValue) {
            var kpisQuery = (
                            from campaign in DbContext.Campaigns
                            where campaign.Id == campaignId.Value
                            select new RecipientStats {
                                Count = (from contact in DbContext.ContactDistributionLists
                                         join distributionList in DbContext.DistributionLists on contact.DistributionListId equals distributionList.Id
                                         where distributionList.Id == campaign.DistributionListId
                                         select contact.ContactId).Count(),
                                Reached = (from messageEvent in DbContext.MessageEvents
                                           where messageEvent.CampaignId == campaign.Id &&
                                                 (
                                                  messageEvent.Type == nameof(MessageEventType.Sent) ||
                                                 (messageEvent.Channel == nameof(MessageChannelKind.Inbox) && messageEvent.Type == nameof(MessageEventType.Created))
                                                 )
                                           select messageEvent.MessageId).Distinct().Count(),
                                Engaged = (from messageEvent in DbContext.MessageEvents
                                           where messageEvent.CampaignId == campaign.Id &&
                                                 (
                                                  messageEvent.Type == nameof(MessageEventType.Opened) ||
                                                  messageEvent.Type == nameof(MessageEventType.Deleted) ||
                                                  messageEvent.Type == nameof(MessageEventType.UnRead) ||
                                                  messageEvent.Type == nameof(MessageEventType.Read)
                                                 )
                                           select messageEvent.ContactId).Distinct().Count()
                            }
                         );
            return await kpisQuery.SingleOrDefaultAsync();
        }
        return new RecipientStats {
            Count = await (from contact in DbContext.ContactDistributionLists
                           select contact.ContactId).Distinct().CountAsync(),
            Reached = await (from messageEvent in DbContext.MessageEvents
                             where messageEvent.Type == nameof(MessageEventType.Sent) ||
                                   (messageEvent.Channel == nameof(MessageChannelKind.Inbox) && messageEvent.Type == nameof(MessageEventType.Created))
                             select messageEvent.MessageId).Distinct().CountAsync(),
            Engaged = await (from messageEvent in DbContext.MessageEvents
                             where messageEvent.Type == nameof(MessageEventType.Opened) ||
                                   messageEvent.Type == nameof(MessageEventType.Deleted) ||
                                   messageEvent.Type == nameof(MessageEventType.UnRead) ||
                                   messageEvent.Type == nameof(MessageEventType.Read)
                             select messageEvent.ContactId).Distinct().CountAsync()
        };
    }

    ///<inheritdoc/>
    public async Task<Dictionary<string, int>> GetDashboardCounters() =>
                await DbContext.MessageEvents
                        .Where(x => x.Type == MessageEventType.Sent.ToString())
                        .GroupBy(m => m.Channel)
                        .ToDictionaryAsync(g => g.Key, g => g.Count());

    /// <inheritdoc />
    public async Task UpdateHit(Guid campaignId) {
        DbContext.Hits.Add(new DbHit {
            CampaignId = campaignId,
            TimeStamp = DateTimeOffset.UtcNow
        });
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Campaign> Publish(Guid campaignId) {
        var campaign = await DbContext
            .Campaigns
            .Include(x => x.Type)
            .Include(x => x.DistributionList)
            .SingleOrDefaultAsync(x => x.Id == campaignId);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(campaignId);
        }
        if (campaign.Published) {
            throw MessageExceptions.CampaignAlreadyPublished(campaignId);
        }
        campaign.Published = true;
        await DbContext.SaveChangesAsync();
        return Mapper.ToCampaign(campaign);
    }
    ///<inheritdoc/>
    public async Task<ResultSet<Recipient>> GetCampaignRecipients(Guid campaignId, ListOptions options) {
        var query = from messageEvent in DbContext.MessageEvents
                    join contact in DbContext.Contacts
                        on messageEvent.ContactId equals contact.Id
                    where messageEvent.CampaignId == campaignId && messageEvent.Type == MessageEventType.Created.ToString()
                    group new { contact, messageEvent } by new {
                        messageEvent.ContactId
                    } into g
                    select new Recipient {
                        Contact = Mapper.ToContact(g.First().contact),
                        CreatedOn = g.First().messageEvent.CreatedOn,
                        Channels = g.Select(x => x.messageEvent).Select(x => x.Channel).ToList()
                    };
        return await query.ToResultSetAsync(options);
    }

    ///<inheritdoc/>
    public async Task<RecipientMessageEvents> GetCampaignRecipientDetails(Guid campaignId, Guid contactId) {
        var details = new RecipientMessageEvents();
        var contact = Mapper.ToContact(await DbContext.Contacts.AsNoTracking().FirstAsync(x => x.Id == contactId));
        var campaign = Mapper.ToCampaign(await DbContext.Campaigns.AsNoTracking().FirstAsync(x => x.Id == campaignId));
        details.Recipient = Mapper.ToContact(await DbContext.Contacts.AsNoTracking().FirstAsync(x => x.Id == contactId));
        GenerateMessageContent(campaign, contact);
        details.Content = campaign.Content;
        details.Events.AddRange(await DbContext.MessageEvents
                        .Where(x => x.CampaignId == campaignId && x.ContactId == contactId)
                        .Select(x => new MessageEvent {
                            Channel = x.Channel,
                            Type = x.Type,
                            CreatedOn = x.CreatedOn
                        })
                        .OrderByDescending(x => x.CreatedOn)
                        .ToListAsync());
        return details;
    }

    private static void GenerateMessageContent(Campaign campaign, Contact? contact) {
        var handlebars = Handlebars.Create();
        handlebars.Configuration.TextEncoder = new HtmlEncoder();
        handlebars.Configuration.UseJson();
        foreach (var content in campaign!.Content) {
            dynamic templateData = new {
                id = campaign.Id,
                title = campaign.Title,
                type = campaign.Type?.Name,
                classification = campaign.Type?.Classification,
                actionLink = new {
                    href = !string.IsNullOrEmpty(campaign.ActionLink?.Href) ? $"_tracking/messages/cta/{(Base64Id)campaign.Id}" : null,
                    text = campaign.ActionLink?.Text,
                },
                mediaBaseHref = campaign.MediaBaseHref,
                now = DateTimeOffset.UtcNow,
                contact = contact is not null
                    ? JsonDocument.Parse(JsonSerializer.Serialize(contact, JsonSerializerOptionDefaults.GetDefaultSettings()))
                    : null,
                data = campaign.Data is not null && (campaign.Data is not string || !string.IsNullOrWhiteSpace(campaign.Data))
                    ? JsonDocument.Parse(JsonSerializer.Serialize(campaign.Data, JsonSerializerOptionDefaults.GetDefaultSettings()))
                    : null
            };
            var messageContent = campaign.Content[content.Key];
            messageContent.Title = handlebars.Compile(content.Value.Title)(templateData);
            messageContent.Body = handlebars.Compile(content.Value.Body)(templateData);
        }
    }
}