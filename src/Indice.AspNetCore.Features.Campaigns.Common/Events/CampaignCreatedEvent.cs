using System;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// Models an event raised when a new campaign is created.
    /// </summary>
    public class CampaignCreatedEvent : IPlatformEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignCreatedEvent"/>.
        /// </summary>
        /// <param name="campaign">Models a campaign when persisted as a queue item.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CampaignCreatedEvent(CampaignQueueItem campaign) {
            Campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
        }

        /// <summary>
        /// Models a campaign when persisted as a queue item.
        /// </summary>
        public CampaignQueueItem Campaign { get; }
    }
}
