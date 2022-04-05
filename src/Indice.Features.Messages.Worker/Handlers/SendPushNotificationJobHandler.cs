using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class SendPushNotificationJobHandler
    {
        public SendPushNotificationJobHandler(
            ILogger<SendPushNotificationJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        }

        public ILogger<SendPushNotificationJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(SendPushNotificationEvent pushNotification) {
            var handler = CampaignJobHandlerFactory.Create<SendPushNotificationEvent>();
            await handler.Process(pushNotification);
        }
    }
}
