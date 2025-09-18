using System.Collections.Generic;
using System.Text.Json;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
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
    public async Task<CampaignDetails?> GetById(Guid id) {
        var campaign = await DbContext
            .Campaigns
            .AsNoTracking()
            .Include(x => x.Attachment)
            .Include(x => x.Type)
            .Include(x => x.DistributionList)
            .Select(Mapper.ProjectToCampaignDetails)
            .SingleOrDefaultAsync(x => x.Id == id);
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
    public async Task Update(Guid id, UpdateCampaignRequest request) {
        var campaign = await DbContext.Campaigns.SingleOrDefaultAsync(x => x.Id == id);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(id);
        }
        if (campaign.Published) {
            throw MessageExceptions.CampaignAlreadyPublished(id);
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
    public async Task Delete(Guid id) {
        var campaign = await DbContext.Campaigns.FindAsync(id);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(id);
        }
        DbContext.Remove(campaign);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<CampaignStatistics?> GetStatistics(Guid id) {
        var campaign = await DbContext.Campaigns.FindAsync(id);
        if (campaign is null) {
            return default;
        }
        var callToActionCount = await DbContext.Hits.AsNoTracking().CountAsync(x => x.CampaignId == id);
        var readCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && x.IsRead);
        var deletedCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && x.IsDeleted);
        int? notReadCount = null;
        if (!campaign.IsGlobal) {
            notReadCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && !x.IsRead);
        }
        var recepientsNumber = await DbContext.CampaignEvent
                                .Where(m => m.CampaignId == id)
                                .Select(m => m.ContactId)
                                .Distinct()
                                .CountAsync();
        var countPerChanel = await DbContext.CampaignEvent.Where(x => x.CampaignId == id && x.Type == MessageEventType.Sent.ToString()).GroupBy(m => m.Channel).ToDictionaryAsync(g => g.Key, g => g.Count());
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
    public async Task UpdateHit(Guid id) {
        DbContext.Hits.Add(new DbHit {
            CampaignId = id,
            TimeStamp = DateTimeOffset.UtcNow
        });
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Campaign> Publish(Guid id) {
        var campaign = await DbContext
            .Campaigns
            .Include(x => x.Type)
            .Include(x => x.DistributionList)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(id);
        }
        if (campaign.Published) {
            throw MessageExceptions.CampaignAlreadyPublished(id);
        }
        campaign.Published = true;
        await DbContext.SaveChangesAsync();
        return Mapper.ToCampaign(campaign);
    }
    ///<inheritdoc/>
    public async Task<Dictionary<string, int>> GetDashboardCounters() =>
                await DbContext.CampaignEvent
                        .Where(x=> x.Type == MessageEventType.Sent.ToString())
                        .GroupBy(m => m.Channel)
                        .ToDictionaryAsync(g => g.Key, g => g.Count());
    ///<inheritdoc/>
    public async Task<ResultSet<Recipient>> GetCampaignRecipients(Guid id, ListOptions options) {
        var query = from messageEvent in DbContext.CampaignEvent
                    join contact in DbContext.Contacts
                        on messageEvent.ContactId equals contact.Id
                    where messageEvent.CampaignId == id && messageEvent.Type == MessageEventType.Created.ToString()
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
    public async Task<RecipientMessageEvents> GetCampaignRecipientDetails(Guid id, Guid contactId) {
        var details = new RecipientMessageEvents();
        var contact = Mapper.ToContact(await DbContext.Contacts.AsNoTracking().FirstAsync(x => x.Id == contactId));
        var campaign = Mapper.ToCampaign(await DbContext.Campaigns.AsNoTracking().FirstAsync(x => x.Id == id));
        details.Recipient = Mapper.ToContact(await DbContext.Contacts.AsNoTracking().FirstAsync(x => x.Id == contactId));
        GenerateMessageContent(campaign, contact);
        details.Content = campaign.Content;
        details.Events.AddRange(await DbContext.CampaignEvent
                        .Where(x => x.CampaignId == id && x.ContactId == contactId)
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