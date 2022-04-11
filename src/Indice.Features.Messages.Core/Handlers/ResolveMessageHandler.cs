using HandlebarsDotNet;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers
{
    /// <summary>
    /// Job handler for <see cref="ResolveMessageEvent"/>.
    /// </summary>
    public class ResolveMessageHandler : ICampaignJobHandler<ResolveMessageEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ResolveMessageHandler"/>.
        /// </summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="contactResolver">Contains information that help gather contact information from other systems.</param>
        /// <param name="contactService">A service that contains contact related operations.</param>
        /// <param name="messageService">A service that contains message related operations.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ResolveMessageHandler(
            Func<string, IEventDispatcher> getEventDispatcher,
            IContactResolver contactResolver,
            IContactService contactService,
            IMessageService messageService
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private IContactResolver ContactResolver { get; }
        private IContactService ContactService { get; }
        private IMessageService MessageService { get; }

        /// <summary>
        /// Decides whether to insert or update a resolved contact.
        /// </summary>
        /// <param name="event">The event model used when a contact is resolved from an external system.</param>
        public async Task Process(ResolveMessageEvent @event) {
            var contact = @event.Contact;
            var contactId = contact.Id;
            if (!string.IsNullOrWhiteSpace(@event.Contact.RecipientId)) {
                contact = await ContactResolver.GetById(@event.Contact.RecipientId);
                // Update contact in campaign's distribution list.
                await ContactService.Update(contactId, Mapper.ToUpdateContactRequest(contact, @event.Campaign.DistributionListId));
            }
            // Make substitution to message content using contact resolved data.
            var handlebars = Handlebars.Create();
            handlebars.Configuration.TextEncoder = new HtmlEncoder();
            foreach (var content in @event.Campaign.Content) {
                // TODO: Make it work with camel case properties.
                dynamic templateData = new { contact, data = @event.Campaign.Data };
                content.Value.Title = handlebars.Compile(content.Value.Title)(templateData);
                content.Value.Body = handlebars.Compile(content.Value.Body)(templateData);
            }
            var campaign = @event.Campaign;
            // Persist message with merged contents.
            await MessageService.Create(new CreateMessageRequest {
                CampaignId = campaign.Id,
                Content = @event.Campaign.Content,
                RecipientId = contact.RecipientId
            });
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.PushNotification)) {
                await eventDispatcher.RaiseEventAsync(SendPushNotificationEvent.FromContactResolutionEvent(@event, broadcast: false),
                    options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendPushNotification));
                return;
            }
            if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.Email)) {
                await eventDispatcher.RaiseEventAsync(SendEmailEvent.FromContactResolutionEvent(@event, broadcast: false),
                    options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendEmail));
                return;
            }
            if (campaign.MessageChannelKind.HasFlag(MessageChannelKind.SMS)) {
                await eventDispatcher.RaiseEventAsync(SendSmsEvent.FromContactResolutionEvent(@event, broadcast: false),
                    options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendSms));
                return;
            }
        }
    }
}
