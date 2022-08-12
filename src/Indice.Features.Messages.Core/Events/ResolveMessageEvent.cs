using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Events
{
    /// <summary>The event model used when a contact is resolved from an external system.</summary>
    public class ResolveMessageEvent
    {
        /// <summary>The event model used when a new campaign is created.</summary>
        public CampaignCreatedEvent Campaign { get; set; }
        /// <summary>The distribution list of the campaign.</summary>
        public Contact Contact { get; set; }

        /// <summary>Creates a <see cref="ResolveMessageEvent"/> instance from a <see cref="CampaignCreatedEvent"/> instance.</summary>
        /// <param name="campaign">Models a campaign.</param>
        /// <param name="contact">Models a contact in the system as a member of a distribution list.</param>
        public static ResolveMessageEvent FromCampaignCreatedEvent(CampaignCreatedEvent campaign, Contact contact) => new() {
            Campaign = campaign,
            Contact = contact
        };
    }
}
