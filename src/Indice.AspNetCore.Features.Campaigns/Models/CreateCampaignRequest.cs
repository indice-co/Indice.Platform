using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Indice.AspNetCore.Features.Campaigns.Data.Models;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to create a new campaign.
    /// </summary>
    public class CreateCampaignRequest : UpdateCampaignRequest
    {
        /// <summary>
        /// The id of the campaign.
        /// </summary>
        [JsonIgnore]
        public Guid Id { get; internal set; }
        /// <summary>
        /// Determines if campaign targets all user base. Defaults to true.
        /// </summary>
        public bool IsGlobal { get; set; } = true;
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> SelectedUserCodes { get; set; } = new List<string>();
        /// <summary>
        /// Defines a CTA (click-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
        /// <summary>
        /// The delivery channel of a campaign. Default is <see cref="CampaignDeliveryChannel.Inbox"/>.
        /// </summary>
        public CampaignDeliveryChannel DeliveryChannel { get; set; } = CampaignDeliveryChannel.Inbox;
    }
}
