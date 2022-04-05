﻿using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>
    /// Options used to filter the list of campaigns.
    /// </summary>
    public class CampaignsFilter
    {
        /// <summary>
        /// The delivery channel of a campaign.
        /// </summary>
        public MessageDeliveryChannel? DeliveryChannel { get; set; }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool? Published { get; set; }
    }
}
