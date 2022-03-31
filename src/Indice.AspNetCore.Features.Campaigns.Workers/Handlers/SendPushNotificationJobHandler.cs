using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class SendPushNotificationJobHandler : CampaignJobHandlerBase
    {
        public SendPushNotificationJobHandler(
            ILogger<SendPushNotificationJobHandler> logger,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<SendPushNotificationJobHandler> Logger { get; }

        public async Task Process(SendPushNotificationEvent pushNotification) => await base.DispatchPushNotification(pushNotification);
    }
}
