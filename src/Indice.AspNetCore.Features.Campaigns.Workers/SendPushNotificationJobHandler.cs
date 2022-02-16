using System.Dynamic;
using Indice.Events;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class SendPushNotificationJobHandler
    {
        public SendPushNotificationJobHandler(Func<string, IPushNotificationService> getPushNotificationService) {
            PushNotificationService = getPushNotificationService(KeyedServiceNames.PushNotificationServiceAzureKey) ?? throw new ArgumentNullException(nameof(getPushNotificationService));
        }

        public IPushNotificationService PushNotificationService { get; }

        public async Task Process(PushNotificationQueueItem pushNotification) {
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            var dataDictionary = data as IDictionary<string, object>;
            if (!dataDictionary.ContainsKey("id")) {
                data.TryAdd("id", pushNotification.Campaign.Id);
            }
            if (pushNotification.Broadcast) {
                await PushNotificationService.BroadcastAsync(pushNotification.Campaign.Title, data, pushNotification.Campaign?.Type?.Name);
            } else {
                await PushNotificationService.SendAsync(pushNotification.Campaign.Title, data, pushNotification.UserCode, classification: pushNotification.Campaign?.Type?.Name);
            }
        }
    }
}
