using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to create a new campaign.
    /// </summary>
    public class CreateCampaignRequest
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
        /// Defines a CTA (call-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
        /// <summary>
        /// The delivery channel of a campaign. Default is <see cref="MessageDeliveryChannel.Inbox"/>.
        /// </summary>
        public MessageDeliveryChannel DeliveryChannel { get; set; } = MessageDeliveryChannel.Inbox;
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public CampaignContent Content { get; set; }
        /// <summary>
        /// Defines a CTA (call-to-action) text.
        /// </summary>
        public string ActionText { get; set; }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool Published { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// The id of the type this campaign belongs.
        /// </summary>
        public Guid? TypeId { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public dynamic Data { get; set; }
    }
}
