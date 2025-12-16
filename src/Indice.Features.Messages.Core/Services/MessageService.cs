using System.Dynamic;
using System.IO;
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
    /// <param name="messageEventQueue">Event queue</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageService(CampaignsDbContext dbContext,
        IOptions<MessageInboxOptions> campaignInboxOptions,
        IContactResolver contactResolver,
        IContactService contactService,
        MessageEventQueue messageEventQueue) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
        ContactService = contactService;
        CampaignInboxOptions = campaignInboxOptions?.Value ?? throw new ArgumentNullException(nameof(campaignInboxOptions));
        MessageEventQueue = messageEventQueue;
    }

    private CampaignsDbContext DbContext { get; }
    private MessageInboxOptions CampaignInboxOptions { get; }
    private IContactResolver ContactResolver { get; }
    private IContactService ContactService { get; }
    private MessageEventQueue MessageEventQueue { get; }

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
            var inboxTitle = string.Empty;
            if (message.Content.TryGetValue(MessageChannelKind.Inbox.ToString(), out var contentValue)) {
                inboxTitle = contentValue.Title ?? "";
            }
            await MessageEventQueue.EnqueueAsync(new MessageEvent() {
                CampaignId = message.CampaignId!.Value,
                ContactId = message.ContactId.Value,
                MessageId = message.Id,
                Type = MessageEventType.Deleted.ToString(),
                Channel = MessageChannelKind.Inbox.ToString(),
                Recipient = recipientId,
                Title = inboxTitle,
                Success = true
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
            await MessageEventQueue.EnqueueAsync(new MessageEvent() {
                CampaignId = message.CampaignId!.Value,
                ContactId = message.ContactId.Value,
                MessageId = message.Id,
                Type = MessageEventType.Read.ToString(),
                Channel = MessageChannelKind.Inbox.ToString(),
                Recipient = recipientId,
                Title = message.GetContentTitle(MessageChannelKind.Inbox),
                Success = true
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

        var contact = await ContactService.GetByRecipientId(recipientId);
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
            CreatedAt = dbCampaign.CreatedAt,
            TypeId = dbCampaign.TypeId
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
                await MessageEventQueue.EnqueueAsync(new MessageEvent() {
                    CampaignId = message.CampaignId!.Value,
                    ContactId = message.ContactId.Value,
                    MessageId = message.Id,
                    Type = MessageEventType.UnRead.ToString(),
                    Channel = MessageChannelKind.Inbox.ToString(),
                    Recipient = recipientId,
                    Title = message.GetContentTitle(MessageChannelKind.Inbox),
                    Success = true
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
            RecipientId = request.RecipientId,
            CreatedAt = DateTimeOffset.UtcNow,
            TypeId = request.TypeId
        };
        DbContext.Messages.Add(dbMessage);
        await DbContext.SaveChangesAsync();

        return dbMessage.Id;
    }



    private IQueryable<Message> GetUserMessagesQuery(string recipientId, MessagesFilter? filter = null, string? searchTerm = null) {
        MessageChannelKind messageChannelKind = GetMessageChannelKind(filter);
        var channelKindKey = messageChannelKind.ToString();

        var globalQuery = GetGlobalCampaignsQuery(recipientId, filter, searchTerm, messageChannelKind);
        var nonGlobalQuery = GetNonGlobalUserMessagesQuery(recipientId, filter, searchTerm, messageChannelKind);

        var resultQuery = globalQuery.Union(nonGlobalQuery);
        var projection = resultQuery.Select(x => ProjectToMessage(x, CampaignInboxOptions.PathPrefix, channelKindKey));
        return projection;
    }

    private static Message ProjectToMessage(CampaignMessagesDto x, string pathPrefix, string channelKindKey) {
        return new Message {
            ActionLink = x.Campaign != null && x.Campaign.ActionLink != null ? new Hyperlink {
                Text = x.Campaign.ActionLink.Text,
                Href = !string.IsNullOrEmpty(x.Campaign.ActionLink.Href)
                            ? $"_tracking/messages/cta/{(Base64Id)x.Campaign.Id}"
                            : null
            } : null,
            ActivePeriod = x.Message != null ? new Period { From = x.Message.CreatedAt } : x.Campaign.ActivePeriod,
            AttachmentUrl = x.Campaign != null && x.Campaign.Attachment != null
                        ? $"{pathPrefix}/messages/attachments/{(Base64Id)x.Campaign.Attachment.Guid}.{Path.GetExtension(x.Campaign.Attachment.Name)!.TrimStart('.')}"
                        : null,
            Title = GetMessageTitle(x, channelKindKey),
            Content = GetMessageContent(x, channelKindKey),
            RequiresSubstitutions = x.Message == null,
            CampaignData = x.Campaign.Data,
            Id = x.Message != null ? x.Message.Id : x.Campaign != null ? x.Campaign.Id : Guid.Empty,
            IsRead = x.Message != null && x.Message.IsRead,
            Type = CreateMessageType(x)
        };
    }

    private static string GetMessageTitle(CampaignMessagesDto x, string channelKindKey) {
        if (x.Message?.Content.ContainsKey(channelKindKey) == true) {
            return x.Message.Content[channelKindKey].Title;
        }

        if (x.Campaign?.Content.ContainsKey(channelKindKey) == true) {
            return x.Campaign.Content[channelKindKey].Title;
        }

        return string.Empty;
    }

    private static string GetMessageContent(CampaignMessagesDto x, string channelKindKey) {
        if (x.Message?.Content.ContainsKey(channelKindKey) == true) {
            return x.Message.Content[channelKindKey].Body;
        }

        if (x.Campaign?.Content.ContainsKey(channelKindKey) == true) {
            return x.Campaign.Content[channelKindKey].Body;
        }

        return string.Empty;
    }
    private static MessageType? CreateMessageType(CampaignMessagesDto x) {
        if (x.Message != null && x.Message.Type != null)
            return new MessageType {
                Id = x.Message.Type.Id,
                Name = x.Message.Type.Name,
                Alias = x.Message.Type.Alias,
                Classification = x.Message.Type.Classification,
            };

        if (x.Campaign != null && x.Campaign.Type != null)
            return new MessageType {
                Id = x.Campaign.Type.Id,
                Name = x.Campaign.Type.Name,
                Alias = x.Campaign.Type.Alias,
                Classification = x.Campaign.Type.Classification,
            };
        return null;
    }
    private static MessageChannelKind GetMessageChannelKind(MessagesFilter? filter) {
        return filter?.MessageChannelKind.HasValue == true && filter.MessageChannelKind != MessageChannelKind.None
            ? filter.MessageChannelKind.Value
            : MessageChannelKind.Inbox;
    }
    private IQueryable<CampaignMessagesDto> GetNonGlobalUserMessagesQuery(string recipientId, MessagesFilter? filter, string? searchTerm, MessageChannelKind messageChannelKind) {
        var query = DbContext.Messages.AsNoTracking().Where(x => x.RecipientId == recipientId).
                                       Select(x => new CampaignMessagesDto { Campaign = x.Campaign, Message = x });
        if (filter is not null) {
            if (filter.TypeId?.Length > 0) {
                query = query.Where(x => x.Message.TypeId != null && filter.TypeId.Contains(x.Message.TypeId.Value));
            }
            if (filter.ActiveFrom.HasValue) {
                query = query.Where(x => (x.Message.CreatedAt) > filter.ActiveFrom.Value);
            }
            if (filter.ActiveTo.HasValue) {
                query = query.Where(x => x.Message.CreatedAt < filter.ActiveTo.Value);
            }
            if (filter.IsRead.HasValue) {
                query = query.Where(x => (x.Message.IsRead == filter.IsRead));
            }
        }

        searchTerm = searchTerm?.Trim();
        if (DbContext.Database.IsSqlServer()) {
            query = query.Where(x => JsonFunctions.JsonValue(x.Message.Content, $"$.{messageChannelKind.ToString().ToLower()}.title") != null);
            if (searchTerm?.Length > 2) {
                query = query.Where(x => JsonFunctions.JsonValue(x.Message.Content, $"$.{messageChannelKind.ToString().ToLower()}.title").Contains(searchTerm));
            }
        } else {

            query = query.Where(x => x.Message.Content.ToString().Contains(messageChannelKind.ToString()));
            if (searchTerm?.Length > 2) {
                query = query.Where(x => x.Message.Content.ToString().Contains(searchTerm));
            }
        }
        return query;
    }


    private IQueryable<CampaignMessagesDto> GetGlobalCampaignsQuery(string recipientId, MessagesFilter? filter, string? searchTerm, MessageChannelKind messageChannelKind) {
        var query = DbContext
                    .Campaigns
                    .AsNoTracking()
                    .Include(x => x.Attachment)
                    .Include(x => x.Type)
                    .SelectMany(
                        collectionSelector: campaign => DbContext.Messages.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.RecipientId == recipientId).DefaultIfEmpty(),
                        resultSelector: (campaign, message) => new CampaignMessagesDto { Campaign = campaign, Message = message }
                    )
                    .Where(x => x.Campaign.Published
                        && (x.Message == null)
                        && (x.Campaign.IsGlobal)
                    );
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
        }
        query = query.Where(x => x.Campaign.MessageChannelKind.HasFlag(messageChannelKind));
        //Free text Search
        if (searchTerm?.Length > 2) {
            query = DbContext.Database.IsSqlServer() ?
             query.Where(x => JsonFunctions.JsonValue(x.Campaign!.Content, $"$.{messageChannelKind.ToString().ToLower()}.title").Contains(searchTerm)) :
             //remember to ask here
             query.Where(x => x.Campaign.Title.Contains(searchTerm));
        }
        return query;
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
