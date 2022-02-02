using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class SendPushNotificationJobHandler
    {
        public SendPushNotificationJobHandler(IPushNotificationService pushNotificationService) {
            PushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
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
