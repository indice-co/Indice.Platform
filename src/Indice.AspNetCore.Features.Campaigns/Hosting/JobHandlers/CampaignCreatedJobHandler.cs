using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    /// <summary>
    /// This job handler executes when a new campaign is created. It checks for campaign's delivery channel and distributes work to a next hop. 
    /// </summary>
    internal class CampaignCreatedJobHandler
    {
        public CampaignCreatedJobHandler(ILogger<CampaignCreatedJobHandler> logger) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<CampaignCreatedJobHandler> Logger { get; }

        public async Task Process(Campaign campaign) {
            if (campaign.DeliveryChannel.HasFlag(CampaignDeliveryChannel.PushNotification)) {

            } else if (campaign.DeliveryChannel.HasFlag(CampaignDeliveryChannel.Email)) {
                // TODO: Create next hop to send campaign via email.
            } else if (campaign.DeliveryChannel.HasFlag(CampaignDeliveryChannel.SMS)) {
                // TODO: Create next hop to send campaign via SMS gateway.
            }
        }
    }
}
