using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="CampaignCreatedEvent"/>.
    /// </summary>
    public class CampaignCreatedHandler : ICampaignJobHandler<CampaignCreatedEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignCreatedHandler"/>.
        /// </summary>
        /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CampaignCreatedHandler(Func<string, IEventDispatcher> getEventDispatcher) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }

        /// <summary>
        /// Distributes a campaign for further processing base on the <see cref="CampaignCreatedEvent.DeliveryChannel"/>.
        /// </summary>
        /// <param name="campaign">The event model used when a new campaign is created.</param>
        public async Task Process(CampaignCreatedEvent campaign) {
            // If campaign is not published, then nothing is sent yet.
            if (!campaign.Published) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.Inbox)) {
                await ProcessInbox(campaign);
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.PushNotification)) {
                await ProcessPushNotifications(campaign);
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.Email)) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.SMS)) {
                return;
            }
        }

        private async Task ProcessInbox(CampaignCreatedEvent campaign) {
            // If campaign is intended for all users, then inbox messages are created upon user interaction (i.e. read a message). 
            if (campaign.IsGlobal) {
                return;
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: InboxDistributionEvent.FromCampaignCreatedEvent(campaign),
                configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.DistributeInbox)
            );
        }

        private async Task ProcessPushNotifications(CampaignCreatedEvent campaign) {
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            // If campaign is intended for all users, then we can broadcast the notification to everyone at once.
            if (campaign.IsGlobal) {
                await eventDispatcher.RaiseEventAsync(
                    payload: new SendPushNotificationEvent {
                        Campaign = campaign,
                        Broadcast = true
                    },
                    configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification)
                );
            } else {
                foreach (var userCode in campaign.SelectedUserCodes) {
                    await eventDispatcher.RaiseEventAsync(
                        payload: new SendPushNotificationEvent {
                            RecipientId = userCode,
                            Campaign = campaign,
                            Broadcast = false
                        },
                        configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification));
                }
            }
        }
    }
}
