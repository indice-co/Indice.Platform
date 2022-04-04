using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="ContactResolutionEvent"/>.
    /// </summary>
    public class ContactResolutionHandler : ICampaignJobHandler<ContactResolutionEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="PersistInboxMessageHandler"/>.
        /// </summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="contactService">A service that contains contact related operations.</param>
        /// <param name="contactResolver">Contains information that help gather contact information from other systems.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ContactResolutionHandler(
            Func<string, IEventDispatcher> getEventDispatcher,
            IContactService contactService,
            IContactResolver contactResolver
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            ContactResolver = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private IContactService ContactService { get; }
        private IContactResolver ContactResolver { get; }

        /// <summary>
        /// Decides whether to insert or update a resolved contact.
        /// </summary>
        /// <param name="event">The event model used when a contact is resolved from an external system.</param>
        public async Task Process(ContactResolutionEvent @event) {
            var contact = await ContactService.GetByRecipientId(@event.RecipientId);
            var isNew = contact is null;
            var needsUpdate = !isNew && DateTimeOffset.UtcNow - contact.UpdatedAt > TimeSpan.FromDays(1);
            if (isNew || needsUpdate) {
                contact = await ContactResolver.Resolve(@event.RecipientId.ToString());
                if (contact is not null) {
                    var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
                    await eventDispatcher.RaiseEventAsync(
                        payload: new UpsertContactEvent(@event.RecipientId, isNew, contact),
                        configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.UpsertContact)
                    );
                }
            }
        }
    }
}
