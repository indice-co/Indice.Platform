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
            Contact contact;
            var recipientId = @event.Contact.RecipientId;
            var isNewContact = false;
            var needsUpdate = false;
            contact = await ContactService.GetByRecipientId(recipientId);
            needsUpdate = contact?.UpdatedAt.HasValue == true && (DateTimeOffset.UtcNow - contact.UpdatedAt.Value) > TimeSpan.FromDays(5);
            if (contact is null) {
                isNewContact = true;
                contact = await ContactResolver.GetById(recipientId);
            }
            if (contact is null) {
                return;
            }
            if (isNewContact) {
                await ContactService.AddToDistributionList(@event.Campaign.DistributionListId.Value, Mapper.ToCreateDistributionListContactRequest(contact));
            }
            if (needsUpdate) {
                await ContactService.Update(contact.Id.Value, Mapper.ToUpdateContactRequest(contact, @event.Campaign.DistributionListId));
            }
            // Make substitution to message content using contact resolved data.
            var handlebars = Handlebars.Create();
            handlebars.Configuration.TextEncoder = new HtmlEncoder();
            var campaign = @event.Campaign;
            foreach (var content in @event.Campaign.Content) {
                // TODO: Make it work with camel case properties.
                dynamic templateData = new { contact, data = @event.Campaign.Data };
                var messageContent = campaign.Content[content.Key];
                messageContent.Title = handlebars.Compile(content.Value.Title)(templateData);
                messageContent.Body = handlebars.Compile(content.Value.Body)(templateData);
            }
            // Persist message with merged contents.
            await MessageService.Create(new CreateMessageRequest {
                CampaignId = campaign.Id,
                Content = campaign.Content,
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
