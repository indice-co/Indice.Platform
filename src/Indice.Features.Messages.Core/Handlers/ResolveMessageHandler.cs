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
            if (!string.IsNullOrWhiteSpace(@event.Contact.RecipientId)) {
                contact = await ContactResolver.GetById(@event.Contact.RecipientId);
                // Persist contact in campaign's distribution list.
                await ContactService.Create(Mapper.ToCreateContactRequest(contact, @event.Campaign.DistributionListId));
            }
            // Make substitution to message content using contact resolved data.
            foreach (var content in @event.Campaign.Content) {
                var template = Handlebars.Compile(content.Value.Body);
                content.Value.Body = template(contact);
            }
            var campaign = @event.Campaign;
            // Persist inbox message with merged content.
            if (campaign.DeliveryChannel.HasFlag(MessageChannelKind.Inbox)) {
                var inboxContent = @event.Campaign.Content[nameof(MessageChannelKind.Inbox)];
                await MessageService.Create(new CreateMessageRequest {
                    Body = inboxContent.Body,
                    CampaignId = campaign.Id,
                    RecipientId = contact.RecipientId,
                    Title = inboxContent.Title
                });
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            if (campaign.DeliveryChannel.HasFlag(MessageChannelKind.PushNotification)) {
                await eventDispatcher.RaiseEventAsync(
                    payload: SendPushNotificationEvent.FromContactResolutionEvent(@event, broadcast: false),
                    configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendPushNotification)
                );
            }
            if (campaign.DeliveryChannel.HasFlag(MessageChannelKind.Email)) {
            }
            if (campaign.DeliveryChannel.HasFlag(MessageChannelKind.SMS)) {
            }
        }
    }
}
