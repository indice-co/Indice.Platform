using System.Collections.Generic;
using Indice.AspNetCore.Features.Campaigns.Data.Models;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to create a new campaign.
    /// </summary>
    public class CreateCampaignRequest : UpdateCampaignRequest
    {
        /// <summary>
        /// Determines if campaign targets all user base.
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> SelectedUserCodes { get; set; } = new List<string>();
        /// <summary>
        /// Defines a CTA (click-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
        /// <summary>
        /// The delivery type of a campaign. Default is <see cref="CampaignDeliveryType.Inbox"/>.
        /// </summary>
        public CampaignDeliveryType DeliveryType { get; set; } = CampaignDeliveryType.Inbox;
    }
}
