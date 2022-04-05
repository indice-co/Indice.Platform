using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class CampaignPublishedJobHandler
    {
        public CampaignPublishedJobHandler(
            ILogger<CampaignPublishedJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory;
        }

        public ILogger<CampaignPublishedJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(CampaignPublishedEvent campaign) {
            var handler = CampaignJobHandlerFactory.Create<CampaignPublishedEvent>();
            await handler.Process(campaign);
        }
    }
}
