using System.Text.Json;
using Azure.Messaging;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Rendering;
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
    /// <param name="partialTemplateResolverFactory">Factory used to create partial template resolvers.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignService(
        CampaignsDbContext dbContext,
        IOptions<MessageManagementOptions> campaignManagementOptions,
        IPartialTemplateResolverFactory partialTemplateResolverFactory
    ) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        CampaignManagementOptions = campaignManagementOptions?.Value ?? throw new ArgumentNullException(nameof(campaignManagementOptions));
        PartialTemplateResolverFactory = partialTemplateResolverFactory ?? throw new ArgumentNullException(nameof(partialTemplateResolverFactory));
    }

    private CampaignsDbContext DbContext { get; }
    private MessageManagementOptions CampaignManagementOptions { get; }
    private IPartialTemplateResolverFactory PartialTemplateResolverFactory { get; }

    /// <inheritdoc />
    public Task<ResultSet<Campaign>> GetList(ListOptions<CampaignListFilter> options) {
        var query = DbContext
                .Campaigns
                .Include(x => x.Type)
                .Include(x => x.DistributionList)
                .AsNoTracking();

        if (options.Filter?.ContactId is Guid contactId) {
            query = from o in query
                    join c in DbContext.ContactDistributionLists on o.DistributionListId equals c.DistributionListId
                    where c.ContactId == contactId
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
        if (options.Filter?.Published is bool published) {
            projectedQuery = projectedQuery.Where(x => x.Published == published);
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
    public async Task<CampaignMetrics> GetMetrics(DateTimeOffset? asOfDate = null) {
        var now = asOfDate ?? DateTimeOffset.UtcNow;
        var campaignMetrics = new CampaignMetrics {
            Total = asOfDate.HasValue ? await DbContext.Campaigns.CountAsync(x => x.CreatedAt <= now) :
                                        await DbContext.Campaigns.CountAsync(),
            // A single statement keeps the count consistent. IX_Campaign_From_To includes Published (see CHANGELOG), so it covers this query.
            Active = await DbContext.Campaigns.CountAsync(x => x.Published &&
                                                               (x.ActivePeriod!.From <= now) && (x.ActivePeriod!.To == null || x.ActivePeriod.To > now)),
            TotalToday = await DbContext.Campaigns.CountAsync(x => x.CreatedAt >= now.Date && x.CreatedAt <= now.AddDays(1).Date),
            TotalYesterday = await DbContext.Campaigns.CountAsync(x => x.CreatedAt >= now.AddDays(-1).Date && x.CreatedAt < now.Date),
        };
        return campaignMetrics;
    }

    /// <inheritdoc />
    public async Task<RecipientMetrics?> GetRecipientMetrics(Guid? campaignId = null) {
        if (campaignId.HasValue) {
            var kpisQuery = (
                            from campaign in DbContext.Campaigns
                            where campaign.Id == campaignId.Value
                            select new RecipientMetrics {
                                TotalCampaigns = 1,
                                Total = campaign.RecipientCount ?? 0,
                                TotalMessages = (from messageEvent in DbContext.MessageEvents
                                                 where messageEvent.CampaignId == campaign.Id &&
                                                       (
                                                        messageEvent.Type == nameof(MessageEventType.Sent) ||
                                                       (messageEvent.Channel == nameof(MessageChannelKind.Inbox) && messageEvent.Type == nameof(MessageEventType.Created))
                                                       )
                                                 select messageEvent.MessageId).Count(),
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

        var campaignCount = await DbContext.Campaigns.CountAsync();
        if (campaignCount == 0) {
            return new RecipientMetrics {
                Total = 0,
                Reached = 0,
                Engaged = 0
            };
        }

        // Sum of the recipients each campaign was sent out to; drafts and global campaigns count as 0.
        var totalRecipientsSum = await DbContext.Campaigns.SumAsync(x => x.RecipientCount ?? 0);
        // Sum of messages and of distinct message ids per campaign that count as "reached", computed in a single pass over the events.
        var reach = await (from e in DbContext.MessageEvents
                           where !e.Orphan &&
                                 (e.Type == nameof(MessageEventType.Sent) ||
                                 (e.Channel == nameof(MessageChannelKind.Inbox) && e.Type == nameof(MessageEventType.Created)))
                           group e by e.CampaignId into g
                           select new {
                               Messages = g.Count(),
                               Reached = g.Select(x => x.MessageId).Distinct().Count()
                           })
                           .GroupBy(x => 1)
                           .Select(g => new {
                               Messages = g.Sum(x => x.Messages),
                               Reached = g.Sum(x => x.Reached)
                           })
                           .FirstOrDefaultAsync();
        var messagesSum = reach?.Messages ?? 0;
        var reachedSum = reach?.Reached ?? 0;

        // Sum of distinct contacts per campaign that count as "engaged"
        var engagedSum = await (from e in DbContext.MessageEvents
                                where !e.Orphan &&
                                      (e.Type == nameof(MessageEventType.Opened) ||
                                      e.Type == nameof(MessageEventType.Deleted) ||
                                      e.Type == nameof(MessageEventType.UnRead) ||
                                      e.Type == nameof(MessageEventType.Read))
                                group e by e.CampaignId into g
                                select g.Select(x => x.ContactId).Distinct().Count()
                                ).SumAsync();

        return new RecipientMetrics {
            TotalCampaigns = campaignCount,
            TotalMessages = messagesSum,
            Total = totalRecipientsSum,
            Reached = reachedSum,
            Engaged = engagedSum
        };
    }

    ///<inheritdoc/>
    public async Task<List<ChannelMetrics>> GetChannelMetrics(Guid? campaignId = null) {
        // A failed send is retried and every attempt logs a "Sent" event, so collapse the attempts of a message on a channel
        // into one outcome: delivered when any attempt succeeded, failed otherwise.
        var query = DbContext.MessageEvents.Where(x => !x.Orphan);

        if (campaignId != null) {
            query = query.Where(x => x.CampaignId == campaignId);
        }

        query = query.Where(x => x.Type == nameof(MessageEventType.Sent) ||
                                 (x.Channel == nameof(MessageChannelKind.Inbox) && x.Type == nameof(MessageEventType.Created)));

        var metrics = await query
            .GroupBy(x => new { x.Channel, x.CampaignId, x.MessageId, x.ContactId })
            .Select(g => new { g.Key.Channel, Delivered = g.Max(x => x.Success ? 1 : 0) })
            .GroupBy(x => x.Channel)
            .Select(g => new { Channel = g.Key, Total = g.Count(), Failures = g.Count(x => x.Delivered == 0) })
            .ToListAsync();
        return metrics.Select(x => new ChannelMetrics {
            Kind = Enum.Parse<MessageChannelKind>(x.Channel),
            Total = x.Total,
            Failures = x.Failures
        }).ToList();
    }


    /// <inheritdoc />
    public async Task<List<Volume<MessageType>>> GetMessageTypeMetrics(DateTimeOffset? onDate = null, int limit = 5) {
        // A message belongs to a single campaign, so the distinct messages are counted per campaign first and then summed per type.
        // This keeps the join with campaigns and the grouping by type down to one row per campaign instead of one per event.
        var perCampaign = from messageEvent in DbContext.MessageEvents
                          where !messageEvent.Orphan &&
                                (onDate == null || (onDate.Value.Date.AddDays(-1) <= messageEvent.CreatedOn && messageEvent.CreatedOn <= onDate.Value.Date.AddDays(1))) &&
                                (messageEvent.Type == nameof(MessageEventType.Sent) ||
                                (messageEvent.Channel == nameof(MessageChannelKind.Inbox) && messageEvent.Type == nameof(MessageEventType.Created)))
                          group messageEvent by messageEvent.CampaignId into g
                          select new { CampaignId = g.Key, Count = g.Select(x => x.MessageId).Distinct().Count() };
        var perType = await (from campaignCount in perCampaign
                             join campaign in DbContext.Campaigns on campaignCount.CampaignId equals campaign.Id
                             group campaignCount by campaign.TypeId into g
                             select new { TypeId = g.Key, Count = g.Sum(x => x.Count) }
                            ).ToListAsync();
        var typeIds = perType.Where(x => x.TypeId.HasValue).Select(x => x.TypeId!.Value).ToList();
        var messageTypes = await DbContext.MessageTypes.Where(x => typeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var items = perType
            .Select(x => {
                var messageType = x.TypeId.HasValue ? messageTypes.GetValueOrDefault(x.TypeId.Value) : null;
                return new Volume<MessageType> {
                    Info = new MessageType {
                        Id = messageType?.Id ?? Guid.Empty,
                        Name = messageType?.Name ?? "None",
                        Classification = messageType?.Classification ?? MessageTypeClassification.System
                    },
                    Total = x.Count
                };
            })
            // Types that no longer exist are reported as "None", same as campaigns without a type.
            .GroupBy(x => x.Info.Id)
            .Select(g => new Volume<MessageType> { Info = g.First().Info, Total = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Total)
            .ToList();
        if (items.Count > limit) {
            items = items.Take(limit).Append(new() {
                Info = new MessageType {
                    Id = Guid.Empty,
                    Name = "Other",
                    Classification = MessageTypeClassification.System
                },
                Total = items.Skip(limit).Sum(x => x.Total)
            }).ToList();
        }
        if (items.Count == 0) {
            return items;
        }
        var maxQuantity = (double)items[0].Total;
        foreach (var item in items) {
            item.Rate = maxQuantity > 0 ? item.Total / maxQuantity : 0.0;
        }
        return items;
    }

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

    /// <inheritdoc />
    public Task SetRecipientCount(Guid campaignId, int recipientCount) =>
        DbContext.Campaigns
            .Where(x => x.Id == campaignId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RecipientCount, recipientCount));
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
        var dbMessage = await DbContext.Messages.AsNoTracking().FirstAsync( x => x.CampaignId == campaignId && x.ContactId == contactId);
        details.Recipient = Mapper.ToContact(await DbContext.Contacts.AsNoTracking().FirstAsync(x => x.Id == contactId));
        var content = new MessageContentDictionary(dbMessage.Content);
        details.Content = content;
        details.Events.AddRange(await DbContext.MessageEvents
                        .Where(x => x.CampaignId == campaignId && x.ContactId == contactId)
                        .Select(x => new MessageEvent {
                            Channel = x.Channel,
                            Type = x.Type,
                            CreatedOn = x.CreatedOn,
                            Recipient = contact.RecipientId!,
                            Title = x.Title,
                            Success = x.Success
                        })
                        .OrderByDescending(x => x.CreatedOn)
                        .ToListAsync());
        return details;
    }

}