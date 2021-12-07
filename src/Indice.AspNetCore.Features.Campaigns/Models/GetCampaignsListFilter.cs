using Indice.AspNetCore.Features.Campaigns.Data.Models;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Options used to filter the list of campaigns.
    /// </summary>
    public class GetCampaignsListFilter
    {
        /// <summary>
        /// The delivery channel of a campaign.
        /// </summary>
        public CampaignDeliveryChannel? DeliveryChannel { get; set; }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool? Published { get; set; }
    }
}
