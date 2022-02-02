using System;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class CampaignCreatedEvent : IPlatformEvent
    {
        public CampaignCreatedEvent(CampaignQueueItem campaign) {
            Campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
        }

        public CampaignQueueItem Campaign { get; }
    }
}
