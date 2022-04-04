using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="PersistInboxMessageEvent"/>.
    /// </summary>
    public class PersistInboxMessageHandler : ICampaignJobHandler<PersistInboxMessageEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="InboxDistributionHandler"/>.
        /// </summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="messageService">A service that contains message related operations.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PersistInboxMessageHandler(
            Func<string, IEventDispatcher> getEventDispatcher,
            IMessageService messageService
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private IMessageService MessageService { get; }

        /// <summary>
        /// Persists a user message in the store.
        /// </summary>
        /// <param name="message">The event model used when persisting a message in the store.</param>
        public async Task Process(PersistInboxMessageEvent message) {
            await MessageService.Create(new CreateMessageRequest {
                Body = message.Body,
                CampaignId = message.Id,
                RecipientId = message.RecipientId,
                Title = message.Title
            });
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: new ContactResolutionEvent(message.RecipientId),
                configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.ContactResolution)
            );
        }
    }
}
