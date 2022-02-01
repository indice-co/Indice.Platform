using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class CampaignCreatedEvent : IPlatformEvent
    {
        public CampaignCreatedEvent(Campaign campaign) {
            Campaign = campaign;
        }

        public Campaign Campaign { get; }
    }
}
