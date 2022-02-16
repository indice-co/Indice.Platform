using Indice.Events;
using Indice.Hosting.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    /// <summary>
    /// This job handler executes when a new campaign is created. It checks for campaign's delivery channel and distributes work accordingly to the next hop.
    /// </summary>
    internal class CampaignCreatedJobHandler
    {
        public CampaignCreatedJobHandler(ILogger<CampaignCreatedJobHandler> logger, IMessageQueue<PushNotificationQueueItem> messageQueue) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public ILogger<CampaignCreatedJobHandler> Logger { get; }
        public IMessageQueue<PushNotificationQueueItem> MessageQueue { get; }

        public async Task Process(CampaignQueueItem campaign) {
            if (!campaign.Published) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(CampaignQueueItem.CampaignDeliveryChannel.PushNotification)) {
                await ProcessPushNotifications(campaign);
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(CampaignQueueItem.CampaignDeliveryChannel.Email)) {
                // TODO: Create next hop to send campaign via email.
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(CampaignQueueItem.CampaignDeliveryChannel.SMS)) {
                // TODO: Create next hop to send campaign via SMS gateway.
                return;
            }
        }

        private async Task ProcessPushNotifications(CampaignQueueItem campaign) {
            if (campaign.IsGlobal) {
                var globalMessage = new PushNotificationQueueItem {
                    Campaign = campaign,
                    Broadcast = true
                };
                await MessageQueue.Enqueue(globalMessage, campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow);
            } else {
                foreach (var userCode in campaign.SelectedUserCodes) {
                    var userMessage = new PushNotificationQueueItem {
                        UserCode = userCode,
                        Campaign = campaign,
                        Broadcast = false
                    };
                    await MessageQueue.Enqueue(userMessage, campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow);
                }
            }
        }
    }
}
