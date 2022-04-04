using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Services;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="InboxDistributionEvent"/>.
    /// </summary>
    public class InboxDistributionHandler : ICampaignJobHandler<InboxDistributionEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="InboxDistributionHandler"/>.
        /// </summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="distributionListService">A service that contains distribution list related operations.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public InboxDistributionHandler(
            Func<string, IEventDispatcher> getEventDispatcher, 
            IDistributionListService distributionListService
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private IDistributionListService DistributionListService { get; }

        /// <summary>
        /// Creates events in order to distribute inbox messages to selected users or users from a distribution list.
        /// </summary>
        /// <param name="inboxDistribution">The event model used when distributing a message to selected users.</param>
        public async Task Process(InboxDistributionEvent inboxDistribution) {
            var recipients = inboxDistribution.SelectedUserCodes ?? new List<string>();
            if (inboxDistribution.DistributionList is not null) {
                var contacts = await DistributionListService.GetContactsList(inboxDistribution.DistributionList.Id, new ListOptions {
                    Page = 1,
                    Size = int.MaxValue
                });
                recipients.AddRange(contacts.Items.Select(x => x.RecipientId));
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            foreach (var id in recipients) {
                await eventDispatcher.RaiseEventAsync(
                    payload: PersistInboxMessageEvent.FromInboxDistributionEvent(inboxDistribution, id),
                    configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.PersistInboxMessage)
                );
            }
        }
    }
}
