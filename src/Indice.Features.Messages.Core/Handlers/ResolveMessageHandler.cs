using System.Dynamic;
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

namespace Indice.Features.Messages.Core.Handlers
{
    /// <summary>Job handler for <see cref="ResolveMessageEvent"/>.</summary>
    public class ResolveMessageHandler : ICampaignJobHandler<ResolveMessageEvent>
    {
        /// <summary>Creates a new instance of <see cref="ResolveMessageHandler"/>.</summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="contactResolver">Contains information that help gather contact information from other systems.</param>
        /// <param name="contactService">A service that contains contact related operations.</param>
        /// <param name="messageService">A service that contains message related operations.</param>
        /// <param name="logger">A logger</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ResolveMessageHandler(
            Func<string, IEventDispatcher> getEventDispatcher,
            IContactResolver contactResolver,
            IContactService contactService,
            IMessageService messageService,
            ILogger<ResolveMessageHandler> logger
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private IContactResolver ContactResolver { get; }
        private IContactService ContactService { get; }
        private IMessageService MessageService { get; }
        private ILogger<ResolveMessageHandler> Logger { get; }

        /// <summary>Decides whether to insert or update a resolved contact.</summary>
        /// <param name="event">The event model used when a contact is resolved from an external system.</param>
        public async Task Process(ResolveMessageEvent @event) {
            var campaign = @event.Campaign;
            Contact contact = null;
            if (!@event.Contact.IsAnonymous) {
                contact = await ContactService.FindByRecipientId(@event.Contact.RecipientId);
                if (contact is null) {
                    contact = await ContactResolver.Resolve(@event.Contact.RecipientId);
                } else if (@event.Contact.IsEmpty) {
                    contact = await ContactResolver.Patch(@event.Contact.RecipientId, contact);
                }
            } else {
                // Anonymous contact should find by email or phone number.
                if (@event.Contact.HasEmail) {
                    contact = await ContactService.FindByEmail(@event.Contact.Email);
                } else if (@event.Contact.HasPhoneNuber) {
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
            if (@event.Contact.NotUpdatedAWhileNow || @event.Contact.IsEmpty) {
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
                    actionLink = new {
                        href = !string.IsNullOrEmpty(campaign.ActionLink.Href) ? $"_tracking/messages/cta/{(Base64Id)campaign.Id}" : null,
                        text = campaign.ActionLink?.Text,
                    },
                    now = DateTimeOffset.UtcNow,
                    contact = JsonSerializer.Deserialize<ExpandoObject>(JsonSerializer.Serialize(contact, JsonSerializerOptionDefaults.GetDefaultSettings()), JsonSerializerOptionDefaults.GetDefaultSettings()),
                    data = JsonSerializer.Deserialize<ExpandoObject>(JsonSerializer.Serialize(campaign.Data, JsonSerializerOptionDefaults.GetDefaultSettings()), JsonSerializerOptionDefaults.GetDefaultSettings())
                };
                var messageContent = campaign.Content[content.Key];
                messageContent.Title = handlebars.Compile(content.Value.Title)(templateData);
                messageContent.Body = handlebars.Compile(content.Value.Body)(templateData);
            }
            // Persist message with merged contents.
            await MessageService.Create(new CreateMessageRequest {
                CampaignId = campaign.Id,
                ContactId = contact.Id,
                Content = campaign.Content,
                RecipientId = contact.RecipientId
            });
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.PushNotification)) {
                await eventDispatcher.RaiseEventAsync(SendPushNotificationEvent.FromContactResolutionEvent(@event, contact, broadcast: false),
                    options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendPushNotification));
            }
            if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.Email)) {
                await eventDispatcher.RaiseEventAsync(SendEmailEvent.FromContactResolutionEvent(@event, contact, broadcast: false),
                    options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendEmail));
            }
            if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.SMS)) {
                await eventDispatcher.RaiseEventAsync(SendSmsEvent.FromContactResolutionEvent(@event, contact, broadcast: false),
                    options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendSms));
            }
        }
    }
}
