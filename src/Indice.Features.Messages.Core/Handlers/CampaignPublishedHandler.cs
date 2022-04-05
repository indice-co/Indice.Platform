using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="CampaignPublishedEvent"/>.
    /// </summary>
    public sealed class CampaignPublishedHandler : ICampaignJobHandler<CampaignPublishedEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignPublishedHandler"/>.
        /// </summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="distributionListService">A service that contains distribution list related operations.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CampaignPublishedHandler(
            Func<string, IEventDispatcher> getEventDispatcher,
            IDistributionListService distributionListService
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private IDistributionListService DistributionListService { get; }

        /// <summary>
        /// Distributes a campaign for further processing base on the <see cref="CampaignPublishedEvent.DeliveryChannel"/>.
        /// </summary>
        /// <param name="campaign">The event model used when a new campaign is created.</param>
        public async Task Process(CampaignPublishedEvent campaign) {
            // If campaign is global and has push notification as delivery channel, then we short-circuit the flow and we immediately broadcast the message.
            if (campaign.IsGlobal && campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.PushNotification)) {
                var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
                await eventDispatcher.RaiseEventAsync(
                    payload: SendPushNotificationEvent.FromCampaignCreatedEvent(campaign, broadcast: true),
                    configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(EventNames.SendPushNotification)
                );
            }
            // If campaign is not global and a distribution list has been set, then we will create multiple events in order to
            // resolve contact info, merge campaign template with contact data and dispatch messages in various channels.
            if (!campaign.IsGlobal && campaign.DistributionListId.HasValue) {
                var contacts = Array.Empty<Contact>();
                var contactsResultSet = await DistributionListService.GetContactsList(campaign.DistributionListId.Value);
                contacts = contactsResultSet.Items;
                var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
                foreach (var contact in contacts) {
                    await eventDispatcher.RaiseEventAsync(
                        payload: ResolveMessageEvent.FromCampaignCreatedEvent(campaign, contact),
                        configure: options => options.WrapInEnvelope(false).WithQueueName(EventNames.ResolveMessage)
                    );
                }
            }
        }
    }
}
