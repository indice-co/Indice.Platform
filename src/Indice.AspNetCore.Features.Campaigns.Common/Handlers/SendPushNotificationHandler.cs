using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="SendPushNotificationEvent"/>.
    /// </summary>
    public class SendPushNotificationHandler : ICampaignJobHandler<SendPushNotificationEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="SendPushNotificationHandler"/>.
        /// </summary>
        /// <param name="getPushNotificationService">Push notification service abstraction in order to support different providers.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SendPushNotificationHandler(Func<string, IPushNotificationService> getPushNotificationService) {
            GetPushNotificationService = getPushNotificationService ?? throw new ArgumentNullException(nameof(getPushNotificationService));
        }

        private Func<string, IPushNotificationService> GetPushNotificationService { get; }

        /// <summary>
        /// Sends a push notification to all users or a single one.
        /// </summary>
        /// <param name="pushNotification">The event model used when sending a push notification.</param>
        public async Task Process(SendPushNotificationEvent pushNotification) {
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            data.TryAdd("campaignId", pushNotification.Campaign.Id);
            var pushNotificationService = GetPushNotificationService(KeyedServiceNames.PushNotificationServiceKey);
            var pushContent = pushNotification.Campaign.Content.Push;
            var pushTitle = pushContent?.Title ?? pushNotification.Campaign.Title;
            var pushBody = pushContent?.Body ?? "-";
            if (pushNotification.Broadcast) {
                await pushNotificationService.BroadcastAsync(pushTitle, pushBody, data, pushNotification.Campaign?.Type?.Name);
            } else {
                await pushNotificationService.SendAsync(pushTitle, pushBody, data, pushNotification.RecipientId, classification: pushNotification.Campaign?.Type?.Name);
            }
        }
    }
}
