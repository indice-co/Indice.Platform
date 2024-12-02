using System.Dynamic;
using System.Text.Json;
using HandlebarsDotNet;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
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
    /// <param name="contactResolver"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageService(CampaignsDbContext dbContext, IOptions<MessageInboxOptions> campaignInboxOptions, IContactResolver contactResolver) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
        CampaignInboxOptions = campaignInboxOptions?.Value ?? throw new ArgumentNullException(nameof(campaignInboxOptions));
    }

    private CampaignsDbContext DbContext { get; }
    private MessageInboxOptions CampaignInboxOptions { get; }
    private IContactResolver ContactResolver { get; }

    /// <inheritdoc />
    public async Task<ResultSet<Message>> GetList(string recipientId, ListOptions<MessagesFilter> options) {
        var userMessages = await GetUserMessagesQuery(recipientId, options?.Filter).ToResultSetAsync(options);
        if (userMessages?.Items != null && userMessages.Items.Any(i => i.RequiresSubstitutions)) {
            await ApplyHandlebarsSubstitutions(recipientId, userMessages);
        }
        return userMessages;
    }

    /// <inheritdoc />
    public async Task<Message> GetById(Guid id, string recipientId, MessageChannelKind? channel = MessageChannelKind.Inbox) {
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
            var dbCampaign = await DbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id);
            if (dbCampaign is null) {
                throw MessageExceptions.MessageNotFound(id);
            }
            var dbMessage = new DbMessage {
                CampaignId = id,
                DeleteDate = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                IsDeleted = true,
                RecipientId = recipientId
            };
            dbMessage.Content = await GetMessageContent(recipientId, dbCampaign);
            DbContext.Messages.Add(dbMessage);
        }
        await DbContext.SaveChangesAsync();
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
            var dbCampaign = await DbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id);
            if (dbCampaign is null) {
                throw MessageExceptions.MessageNotFound(id);
            }
            var dbMessage = new DbMessage {
                CampaignId = id,
                Id = Guid.NewGuid(),
                IsRead = true,
                ReadDate = DateTime.UtcNow,
                RecipientId = recipientId
            };
            dbMessage.Content = await GetMessageContent(recipientId, dbCampaign);
            DbContext.Messages.Add(dbMessage);
        }
        await DbContext.SaveChangesAsync();
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

    private IQueryable<Message> GetUserMessagesQuery(string recipientId, MessagesFilter filter = null) {
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
                query = query.Where(x => !x.Campaign.ActivePeriod.To.HasValue || x.Campaign.ActivePeriod.To.Value >= DateTime.UtcNow);
            }
            if (filter.TypeId.Length > 0) {
                query = query.Where(x => x.Campaign.Type != null && filter.TypeId.Contains(x.Campaign.Type.Id));
            }
            if (filter.ActiveFrom.HasValue) {
                query = query.Where(x => (x.Campaign.ActivePeriod.From ?? DateTimeOffset.MaxValue) > filter.ActiveFrom.Value);
            }
            if (filter.ActiveTo.HasValue) {
                query = query.Where(x => (x.Campaign.ActivePeriod.To ?? DateTimeOffset.MinValue) < filter.ActiveTo.Value);
            }
            if (filter.IsRead.HasValue) {
                query = query.Where(x => ((bool?)x.Message.IsRead ?? false) == filter.IsRead);
            }
            if (filter.MessageChannelKind.HasValue && filter.MessageChannelKind != MessageChannelKind.None) {
                messageChannelKind = filter.MessageChannelKind.Value;
            }
        }
        query = query.Where(x => x.Campaign.MessageChannelKind.HasFlag(messageChannelKind));
        var messageChannelKindKey = messageChannelKind.ToString();
        return query.Select(x => new Message {
            ActionLink = x.Campaign.ActionLink != null ? new Hyperlink {
                Text = x.Campaign.ActionLink.Text,
                Href = !string.IsNullOrEmpty(x.Campaign.ActionLink.Href)
                    ? $"_tracking/messages/cta/{(Base64Id)x.Campaign.Id}"
                    : null
            } : null,
            ActivePeriod = x.Campaign.ActivePeriod,
            AttachmentUrl = x.Campaign.Attachment != null
                ? $"{CampaignInboxOptions.PathPrefix}/messages/attachments/{(Base64Id)x.Campaign.Attachment.Guid}.{Path.GetExtension(x.Campaign.Attachment.Name).TrimStart('.')}"
                : null,
            // TODO: Fix substitution when message is null.
            Title = x.Message != null && x.Message.Content.ContainsKey(messageChannelKindKey) 
                ? x.Message.Content[messageChannelKindKey].Title 
                : x.Campaign != null && x.Campaign.Content.ContainsKey(messageChannelKindKey) ? x.Campaign.Content[messageChannelKindKey].Title : string.Empty,
            Content = x.Message != null && x.Message.Content.ContainsKey(messageChannelKindKey) 
                ? x.Message.Content[messageChannelKindKey].Body 
                : x.Campaign != null && x.Campaign.Content.ContainsKey(messageChannelKindKey) ? x.Campaign.Content[messageChannelKindKey].Body : string.Empty,
            CreatedAt = x.Campaign.CreatedAt,
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

    private async Task<MessageContentDictionary> GetMessageContent(string userIdentitfier, DbCampaign dbCampaign) {
        if (dbCampaign.MessageChannelKind.HasFlag(MessageChannelKind.Inbox) && dbCampaign.Content.ContainsKey(MessageChannelKind.Inbox.ToString())) {
            var handlebars = Handlebars.Create();
            handlebars.Configuration.TextEncoder = new HtmlEncoder();
            var contact = await ContactResolver.Resolve(userIdentitfier);
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
