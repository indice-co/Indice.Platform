using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class SendPushNotificationJobHandler : CampaignJobHandlerBase
    {
        public SendPushNotificationJobHandler(
            Func<string, IEventDispatcher> getEventDispatcher,
            Func<string, IPushNotificationService> getPushNotificationService
        ) : base(getEventDispatcher, getPushNotificationService) { }

        public IPushNotificationService PushNotificationService { get; }

        public async Task Process(PushNotificationQueueItem pushNotification) => await base.DispatchPushNotification(pushNotification);
    }
}
