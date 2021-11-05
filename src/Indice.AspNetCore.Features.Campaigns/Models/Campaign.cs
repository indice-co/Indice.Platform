using System;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Models a campaign.
    /// </summary>
    public class Campaign
    {
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; set; }
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
        /// Defines a CTA (click-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
        /// <summary>
        /// Specifies when a campaign was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>
        /// Determines if a campaign is active.
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// Determines if campaign targets all user base.
        /// </summary>
        public bool IsGlobal { get; set; }
        public bool IsNotification { get; set; }
    }
}