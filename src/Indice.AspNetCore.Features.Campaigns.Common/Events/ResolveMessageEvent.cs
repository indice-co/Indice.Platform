using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when a contact is resolved from an external system.
    /// </summary>
    public class ResolveMessageEvent
    {
        /// <summary>
        /// The event model used when a new campaign is created.
        /// </summary>
        public CampaignPublishedEvent Campaign { get; set; }
        /// <summary>
        /// The distribution list of the campaign.
        /// </summary>
        public Contact Contact { get; set; }

        /// <summary>
        /// Creates a <see cref="ResolveMessageEvent"/> instance from a <see cref="CampaignPublishedEvent"/> instance.
        /// </summary>
        /// <param name="campaign">Models a campaign.</param>
        /// <param name="contact">Models a contact in the system as a member of a distribution list.</param>
        public static ResolveMessageEvent FromCampaignCreatedEvent(CampaignPublishedEvent campaign, Contact contact) => new() {
            Campaign = campaign,
            Contact = contact
        };
    }
}
