﻿using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class CampaignCreatedJobHandler
    {
        public CampaignCreatedJobHandler(
            ILogger<CampaignCreatedJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory;
        }

        public ILogger<CampaignCreatedJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(CampaignCreatedEvent campaign) {
            var handler = CampaignJobHandlerFactory.Create<CampaignCreatedEvent>();
            await handler.Process(campaign);
        }
    }
}
