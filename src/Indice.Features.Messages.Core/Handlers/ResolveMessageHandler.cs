using System.Text.Json;
using HandlebarsDotNet;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Serialization;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="ResolveMessageEvent"/>.</summary>
public class ResolveMessageHandler : ICampaignJobHandler<ResolveMessageEvent>
{
    /// <summary>Creates a new instance of <see cref="ResolveMessageHandler"/>.</summary>
    /// <param name="eventDispatcherFactory">Provides methods that allow application components to communicate with each other by dispatching events.</param>
    /// <param name="contactResolver">Contains information that help gather contact information from other systems.</param>
    /// <param name="contactService">A service that contains contact related operations.</param>
    /// <param name="messageService">A service that contains message related operations.</param>
    /// <param name="logger">A logger</param>
    /// <param name="options">Configuration for workers.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ResolveMessageHandler(
        IEventDispatcherFactory eventDispatcherFactory,
        IContactResolver contactResolver,
        IContactService contactService,
        IMessageService messageService,
        ILogger<ResolveMessageHandler> logger,
        Microsoft.Extensions.Options.IOptions<MessageWorkerOptions> options
    ) {
        EventDispatcherFactory = eventDispatcherFactory ?? throw new ArgumentNullException(nameof(eventDispatcherFactory));
        ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
        ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private IEventDispatcherFactory EventDispatcherFactory { get; }
    private IContactResolver ContactResolver { get; }
    private IContactService ContactService { get; }
    private IMessageService MessageService { get; }
    private ILogger<ResolveMessageHandler> Logger { get; }
    private MessageWorkerOptions Options { get; }

    /// <summary>Decides whether to insert or update a resolved contact.</summary>
    /// <param name="event">The event model used when a contact is resolved from an external system.</param>
    public async Task Process(ResolveMessageEvent @event) {
        var campaign = @event.Campaign;
        Contact contact = null;
        var contactNotUpdatedAWhileNow = !@event.Contact.UpdatedAt.HasValue
            || (DateTimeOffset.UtcNow - @event.Contact.UpdatedAt.Value) > TimeSpan.FromDays(Options.ContactRetainPeriodInDays);
        if (!@event.Contact.IsAnonymous) {
            contact = await ContactService.FindByRecipientId(@event.Contact.RecipientId);
            if (contact is null) {
                contact = await ContactResolver.Resolve(@event.Contact.RecipientId);
            } else if (contactNotUpdatedAWhileNow || @event.Contact.IsEmpty) {
                contact = await ContactResolver.Patch(@event.Contact.RecipientId, contact);
            }
        } else {
            // Anonymous contact should find by email or phone number.
            if (@event.Contact.HasEmail) {
                contact = await ContactService.FindByEmail(@event.Contact.Email);
            } else if (@event.Contact.HasPhoneNumber) {
                contact = await ContactService.FindByPhoneNumber(@event.Contact.PhoneNumber);
            }
            // If found but is already anonymous try to patch with filled data.
            if (contact is not null && contact.IsAnonymous) {
                contact.LastName = string.IsNullOrEmpty(contact.LastName) ? @event.Contact.LastName : contact.LastName;
                contact.FirstName = string.IsNullOrEmpty(contact.FirstName) ? @event.Contact.FirstName : contact.FirstName;
                contact.FullName = string.IsNullOrEmpty(contact.FullName) ? @event.Contact.FullName : contact.FullName;
                contact.PhoneNumber = string.IsNullOrEmpty(contact.PhoneNumber) ? @event.Contact.PhoneNumber : contact.PhoneNumber;
                contact.Email = string.IsNullOrEmpty(contact.Email) ? @event.Contact.Email : contact.Email;
            }
        }
        contact ??= @event.Contact;
        if ((contactNotUpdatedAWhileNow || @event.Contact.IsEmpty) && contact.Id.HasValue) {
            await ContactService.Update(contact.Id.Value, Mapper.ToUpdateContactRequest(contact, campaign.DistributionListId));
        }
        // In case this is not yet published we should stop here so no messages get sent yet.
        if (!@event.Campaign.Published) {
            return;
        }
        // Make substitution to message content using contact resolved data.
        var handlebars = Handlebars.Create();
        handlebars.Configuration.TextEncoder = new HtmlEncoder();
        foreach (var content in campaign.Content) {
            dynamic templateData = new {
                id = campaign.Id,
                title = campaign.Title,
                type = campaign.Type?.Name,
                commecial = campaign.Type?.Commercial,
                actionLink = new {
                    href = !string.IsNullOrEmpty(campaign.ActionLink?.Href) ? $"_tracking/messages/cta/{(Base64Id)campaign.Id}" : null,
                    text = campaign.ActionLink?.Text,
                },
                mediaBaseHref = campaign.MediaBaseHref,
                now = DateTimeOffset.UtcNow,
                contact = contact is not null
                    ? JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(JsonSerializer.Serialize(contact, JsonSerializerOptionDefaults.GetDefaultSettings()), JsonSerializerOptionDefaults.GetDefaultSettings())
                    : null,
                data = campaign.Data is not null && (campaign.Data is not string || !string.IsNullOrWhiteSpace(campaign.Data))
                    ? JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(campaign.Data, JsonSerializerOptionDefaults.GetDefaultSettings())
                    : null
            };
            var messageContent = campaign.Content[content.Key];
            messageContent.Title = handlebars.Compile(content.Value.Title)(templateData);
            messageContent.Body = handlebars.Compile(content.Value.Body)(templateData);
        }
        // Persist message with merged contents.
        var messageId = await MessageService.Create(new CreateMessageRequest {
            CampaignId = campaign.Id,
            ContactId = contact.Id,
            Content = campaign.Content,
            RecipientId = contact.RecipientId
        });
        var eventDispatcher = EventDispatcherFactory.Create(KeyedServiceNames.EventDispatcherServiceKey);
        if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.PushNotification)) {
            await eventDispatcher.RaiseEventAsync(SendPushNotificationEvent.FromContactResolutionEvent(@event, contact, broadcast: false, messageId: messageId),
                options => options.WrapInEnvelope().At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendPushNotification));
        }
        if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.Email)) {
            await eventDispatcher.RaiseEventAsync(SendEmailEvent.FromContactResolutionEvent(@event, contact, broadcast: false),
                options => options.WrapInEnvelope().At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendEmail));
        }
        if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.SMS)) {
            await eventDispatcher.RaiseEventAsync(SendSmsEvent.FromContactResolutionEvent(@event, contact, broadcast: false),
                options => options.WrapInEnvelope().At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendSms));
        }
    }
}
