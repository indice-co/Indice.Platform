using System;
using System.Dynamic;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to update an existing campaign.
    /// </summary>
    public class UpdateCampaignRequest
    {
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Defines a CTA (click-to-action) text.
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
        public ExpandoObject Data { get; set; }
    }
}
