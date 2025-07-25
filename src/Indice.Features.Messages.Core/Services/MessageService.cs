using System.Dynamic;
using System.Text.Json;
using HandlebarsDotNet;
using Indice.EntityFrameworkCore.Functions;
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

/// <summary>An implementation of <see cref="IMessageService"/> for Entity Framework Core.</summary>
public class MessageService : IMessageService
{
    /// <summary>Creates a new instance of <see cref="MessageService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <param name="campaignInboxOptions">Options used to configure the Campaigns inbox API feature.</param>
    /// <param name="contactResolver">Contact resolver service</param>
    /// <param name="contactService"></param>
    /// <param name="campaignEventQueue">Event queue</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageService(CampaignsDbContext dbContext,
        IOptions<MessageInboxOptions> campaignInboxOptions,
        IContactResolver contactResolver,
        IContactService contactService,
        CampaignEventQueue campaignEventQueue) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
        ContactService = contactService;
        CampaignInboxOptions = campaignInboxOptions?.Value ?? throw new ArgumentNullException(nameof(campaignInboxOptions));
        CampaignEventQueue = campaignEventQueue;
    }

    private CampaignsDbContext DbContext { get; }
    private MessageInboxOptions CampaignInboxOptions { get; }
    private IContactResolver ContactResolver { get; }
    private IContactService ContactService { get; }
    private CampaignEventQueue CampaignEventQueue { get; }

    /// <inheritdoc />
    public async Task<ResultSet<Message>?> GetList(string recipientId, ListOptions<MessagesFilter>? options) {
        var userMessages = await GetUserMessagesQuery(recipientId, options?.Filter, options?.Search).ToResultSetAsync(options);
        if (userMessages?.Items != null && userMessages.Items.Any(i => i.RequiresSubstitutions)) {
            await ApplyHandlebarsSubstitutions(recipientId, userMessages);
        }
        return userMessages;
    }

    /// <inheritdoc />
    public async Task<Message?> GetById(Guid id, string recipientId, MessageChannelKind? channel = MessageChannelKind.Inbox) {
        var userMessage = await GetUserMessagesQuery(recipientId, new MessagesFilter { MessageChannelKind = channel }).SingleOrDefaultAsync(x => x.Id == id);
        if (userMessage?.RequiresSubstitutions == true && channel == MessageChannelKind.Inbox) {
            await ApplyHandlebarsSubstitutions(recipientId, userMessage);
        }
        return userMessage;
    }

    /// <inheritdoc />
    public async Task MarkAsDeleted(Guid id, string recipientId) {
        var message = await DbContext.Messages
            .SingleOrDefaultAsync(x => x.CampaignId == id && x.RecipientId == recipientId);
        if (message is not null) {
            if (message.IsDeleted) {
                throw MessageExceptions.MessageAlreadyRead(id);
            }
            message.IsDeleted = true;
            message.DeleteDate = DateTime.UtcNow;
        } else {
            message = await CreateMessageAndMarkAsDeleted(id, recipientId);
        }

        if (message.ContactId.HasValue) {
            await CampaignEventQueue.EnqueueAsync(new MessageEvent() {
                CampaignId = message.CampaignId,
                ContactId = message.ContactId.Value,
                MessageId = message.Id,
                Type = MessageEventType.MarkedAsDeleted.ToString(),
                Channel = MessageChannelKind.Inbox.ToString()
            });
        }
        await DbContext.SaveChangesAsync();
    }

    private async Task<DbMessage> CreateMessageAndMarkAsDeleted(Guid id, string recipientId) {
        var dbMessage = await CreateMessage(id, recipientId);
        dbMessage.DeleteDate = DateTime.UtcNow;
        dbMessage.IsDeleted = true;
        DbContext.Messages.Add(dbMessage);
        return dbMessage;
    }

    /// <inheritdoc />
    public async Task MarkAsRead(Guid id, string recipientId) {
        var message = await DbContext.Messages
            .SingleOrDefaultAsync(x => x.CampaignId == id && x.RecipientId == recipientId);
        if (message is not null) {
            if (message.IsRead) {
                throw MessageExceptions.MessageAlreadyRead(id);
            }
            message.IsRead = true;
            message.ReadDate = DateTime.UtcNow;
        } else {
            message = await CreateMessageAndMarkAsRead(id, recipientId);
        }
        if (message.ContactId.HasValue) {
            await CampaignEventQueue.EnqueueAsync(new MessageEvent() {
                CampaignId = message.CampaignId,
                ContactId = message.ContactId.Value,
                MessageId = message.Id,
                Type = MessageEventType.MarkedAsRead.ToString(),
                Channel = MessageChannelKind.Inbox.ToString()
            });
        }
        await DbContext.SaveChangesAsync();
    }

    private async Task<DbMessage> CreateMessageAndMarkAsRead(Guid id, string recipientId) {
        var dbMessage = await CreateMessage(id, recipientId);
        dbMessage.IsRead = true;
        dbMessage.ReadDate = DateTime.UtcNow;
        DbContext.Messages.Add(dbMessage);
        return dbMessage;
    }

    private async Task<DbMessage> CreateMessage(Guid id, string recipientId) {
        var dbCampaign = await DbContext.Campaigns
                                .Include(x => x.DistributionList)
                                .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw MessageExceptions.MessageNotFound(id);

        var contact = await ContactService.FindByRecipientId(recipientId);
        if (dbCampaign.DistributionListId.HasValue) {
            if (contact is null) {
                var resolvedContact = await ContactResolver.Resolve(recipientId) ??
                    throw MessageExceptions.ContantResolverNotFound(recipientId);
                contact = await ContactService.Create(Mapper.ToCreateContactRequest(resolvedContact));
            }
            dbCampaign.DistributionList.ContactDistributionLists.Add(new DbDistributionListContact {
                DistributionListId = dbCampaign.DistributionListId!.Value,
                ContactId = contact.Id!.Value
            });
        }

        var dbMessage = new DbMessage {
            CampaignId = id,
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Content = GetMessageContent(dbCampaign, contact),
            ContactId = contact?.Id,
        };
        return dbMessage;
    }


    /// <inheritdoc />
    public async Task MarkAsUnread(Guid id, string recipientId) {
        var message = await DbContext.Messages
            .SingleOrDefaultAsync(x => x.CampaignId == id && x.RecipientId == recipientId);
        if (message is not null) {
            if (!message.IsRead) {
                throw MessageExceptions.MessageAlreadyUnread(id);
            }
            message.IsRead = false;
            message.ReadDate = null;
            if (message.ContactId.HasValue) {
                await CampaignEventQueue.EnqueueAsync(new MessageEvent() {
                    CampaignId = message.CampaignId,
                    ContactId = message.ContactId.Value,
                    MessageId = message.Id,
                    Type = MessageEventType.MarkedAsUnread.ToString(),
                    Channel = MessageChannelKind.Inbox.ToString()
                });
            }
            await DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<Guid> Create(CreateMessageRequest request) {
        var dbMessage = new DbMessage {
            CampaignId = request.CampaignId,
            ContactId = request.ContactId,
            Content = request.Content,
            Id = Guid.NewGuid(),
            RecipientId = request.RecipientId
        };
        DbContext.Messages.Add(dbMessage);
        await DbContext.SaveChangesAsync();

        return dbMessage.Id;
    }

    private IQueryable<Message> GetUserMessagesQuery(string recipientId, MessagesFilter? filter = null, string? searchTerm = null) {
        var query = DbContext
            .Campaigns
            .AsNoTracking()
            .Include(x => x.Attachment)
            .Include(x => x.Type)
            .SelectMany(
                collectionSelector: campaign => DbContext.Messages.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.RecipientId == recipientId).DefaultIfEmpty(),
                resultSelector: (campaign, message) => new { Campaign = campaign, Message = message }
            )
            .Where(x => x.Campaign.Published
                && (x.Message == null || !x.Message.IsDeleted)
                && (x.Campaign.IsGlobal || x.Message != null && x.Message.RecipientId == recipientId)
            );
        var messageChannelKind = MessageChannelKind.Inbox;
        if (filter is not null) {
            if (filter.ShowExpired.HasValue) {
                query = query.Where(x => !x.Campaign.ActivePeriod!.To.HasValue || x.Campaign.ActivePeriod.To.Value >= DateTime.UtcNow);
            }
            if (filter.TypeId?.Length > 0) {
                query = query.Where(x => x.Campaign.Type != null && filter.TypeId.Contains(x.Campaign.Type.Id));
            }
            if (filter.ActiveFrom.HasValue) {
                query = query.Where(x => (x.Campaign.ActivePeriod!.From ?? DateTimeOffset.MaxValue) > filter.ActiveFrom.Value);
            }
            if (filter.ActiveTo.HasValue) {
                query = query.Where(x => (x.Campaign.ActivePeriod!.To ?? DateTimeOffset.MinValue) < filter.ActiveTo.Value);
            }
            if (filter.IsRead.HasValue) {
                query = query.Where(x => ((bool?)x.Message!.IsRead ?? false) == filter.IsRead);
            }
            if (filter.MessageChannelKind.HasValue && filter.MessageChannelKind != MessageChannelKind.None) {
                messageChannelKind = filter.MessageChannelKind.Value;
            }
        }
        query = query.Where(x => x.Campaign.MessageChannelKind.HasFlag(messageChannelKind));
        var channelKindKey = messageChannelKind.ToString();
        //Free text Search
        searchTerm = searchTerm?.Trim();

        if (searchTerm?.Length > 2) {
            query = DbContext.Database.IsSqlServer() ?
             query.Where(x => JsonFunctions.JsonValue(x.Message!.Content, $"$.{channelKindKey.ToLower()}.title").Contains(searchTerm)) :
             query.Where(x => x.Campaign.Title.Contains(searchTerm));
        }

        return query.Select(x => new Message {
            ActionLink = x.Campaign.ActionLink != null ? new Hyperlink {
                Text = x.Campaign.ActionLink.Text,
                Href = !string.IsNullOrEmpty(x.Campaign.ActionLink.Href)
                    ? $"_tracking/messages/cta/{(Base64Id)x.Campaign.Id}"
                    : null
            } : null,
            ActivePeriod = x.Campaign.ActivePeriod,
            AttachmentUrl = x.Campaign.Attachment != null
                ? $"{CampaignInboxOptions.PathPrefix}/messages/attachments/{(Base64Id)x.Campaign.Attachment.Guid}.{Path.GetExtension(x.Campaign.Attachment.Name)!.TrimStart('.')}"
                : null,
            // TODO: Fix substitution when message is null.
            Title = x.Message != null && x.Message.Content.ContainsKey(channelKindKey)
                ? x.Message.Content[channelKindKey].Title
                : x.Campaign != null && x.Campaign.Content.ContainsKey(channelKindKey) ? x.Campaign.Content[channelKindKey].Title : string.Empty,
            Content = x.Message != null && x.Message.Content.ContainsKey(channelKindKey)
                ? x.Message.Content[channelKindKey].Body
                : x.Campaign != null && x.Campaign.Content.ContainsKey(channelKindKey) ? x.Campaign.Content[channelKindKey].Body : string.Empty,
            CreatedAt = x.Campaign!.CreatedAt,
            RequiresSubstitutions = x.Message == null,
            CampaignData = x.Campaign.Data,
            Id = x.Campaign.Id,
            IsRead = x.Message != null && x.Message.IsRead,
            Type = x.Campaign.Type != null ? new MessageType {
                Id = x.Campaign.Type.Id,
                Name = x.Campaign.Type.Name,
                Classification = x.Campaign.Type.Classification,
            } : null
        });
    }

    private async Task ApplyHandlebarsSubstitutions(string userIdentitfier, ResultSet<Message> userMessages) {
        var handlebars = Handlebars.Create();
        handlebars.Configuration.TextEncoder = new HtmlEncoder();
        var contact = await ContactResolver.Resolve(userIdentitfier);
        var contactExpandoObject = contact is not null
            ? JsonSerializer.Deserialize<ExpandoObject>(JsonSerializer.Serialize(contact, JsonSerializerOptionDefaults.GetDefaultSettings()), JsonSerializerOptionDefaults.GetDefaultSettings())
            : null;
        foreach (var message in userMessages.Items.Where(i => i.RequiresSubstitutions)) {
            dynamic templateData = new {
                contact = contactExpandoObject,
                data = message.CampaignData is not null && (message.CampaignData is not string || !string.IsNullOrWhiteSpace(message.CampaignData))
                        ? JsonSerializer.Deserialize<ExpandoObject>(message.CampaignData, JsonSerializerOptionDefaults.GetDefaultSettings())
                        : null
            };
            message.Title = handlebars.Compile(message.Title)(templateData);
            message.Content = handlebars.Compile(message.Content)(templateData);
        }
    }

    private async Task ApplyHandlebarsSubstitutions(string userIdentitfier, Message userMessage) {
        var handlebars = Handlebars.Create();
        handlebars.Configuration.TextEncoder = new HtmlEncoder();
        var contact = await ContactResolver.Resolve(userIdentitfier);
        dynamic templateData = new {
            contact = contact is not null
                        ? JsonSerializer.Deserialize<ExpandoObject>(JsonSerializer.Serialize(contact, JsonSerializerOptionDefaults.GetDefaultSettings()), JsonSerializerOptionDefaults.GetDefaultSettings())
                        : null,
            data = userMessage.CampaignData is not null && (userMessage.CampaignData is not string || !string.IsNullOrWhiteSpace(userMessage.CampaignData))
                        ? JsonSerializer.Deserialize<ExpandoObject>(userMessage.CampaignData, JsonSerializerOptionDefaults.GetDefaultSettings())
                        : null
        };
        userMessage.Title = handlebars.Compile(userMessage.Title)(templateData);
        userMessage.Content = handlebars.Compile(userMessage.Content)(templateData);
    }

    private MessageContentDictionary GetMessageContent(DbCampaign dbCampaign, Contact? contact) {
        if (dbCampaign.MessageChannelKind.HasFlag(MessageChannelKind.Inbox) && dbCampaign.Content.ContainsKey(MessageChannelKind.Inbox.ToString())) {
            var handlebars = Handlebars.Create();
            handlebars.Configuration.TextEncoder = new HtmlEncoder();
            dynamic templateData = new {
                contact = contact is not null
                            ? JsonSerializer.Deserialize<ExpandoObject>(JsonSerializer.Serialize(contact, JsonSerializerOptionDefaults.GetDefaultSettings()), JsonSerializerOptionDefaults.GetDefaultSettings())
                            : null,
                data = dbCampaign.Data is not null && (dbCampaign.Data is not string || !string.IsNullOrWhiteSpace(dbCampaign.Data))
                            ? JsonSerializer.Deserialize<ExpandoObject>(dbCampaign.Data, JsonSerializerOptionDefaults.GetDefaultSettings())
                            : null
            };
            var messageContent = dbCampaign.Content[MessageChannelKind.Inbox.ToString()];
            messageContent.Title = handlebars.Compile(messageContent.Title)(templateData);
            messageContent.Body = handlebars.Compile(messageContent.Body)(templateData);
        }
        return dbCampaign.Content;
    }
}
