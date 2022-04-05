using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when sending a push notification.
    /// </summary>
    public class SendPushNotificationEvent
    {
        /// <summary>
        /// The id of the campaign.
        /// </summary>
        public Guid CampaignId { get; set; }
        /// <summary>
        /// The title of the message.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public ExpandoObject Data { get; set; }
        /// <summary>
        /// Defines if push notification is sent to all registered user devices.
        /// </summary>
        public bool Broadcast { get; set; }
        /// <summary>
        /// The id of the recipient.
        /// </summary>
        public string RecipientId { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Creates a <see cref="SendPushNotificationEvent"/> instance from a <see cref="CampaignPublishedEvent"/> instance.
        /// </summary>
        /// <param name="campaign">Models a contact in the system as a member of a distribution list.</param>
        /// <param name="broadcast">Defines if push notification is sent to all registered user devices.</param>
        /// <param name="recipientId">The id of the recipient.</param>
        public static SendPushNotificationEvent FromCampaignCreatedEvent(CampaignPublishedEvent campaign, bool broadcast, string recipientId = null) => new() {
            Body = "",
            Broadcast = broadcast,
            CampaignId = campaign.Id,
            Data = campaign.Data,
            MessageType = campaign.Type,
            RecipientId = recipientId,
            Title = ""
        };

        /// <summary>
        /// Creates a <see cref="SendPushNotificationEvent"/> instance from a <see cref="ResolveMessageEvent"/> instance.
        /// </summary>
        /// <param name="contact">The event model used when a contact is resolved from an external system.</param>
        /// <param name="broadcast">Defines if push notification is sent to all registered user devices.</param>
        public static SendPushNotificationEvent FromContactResolutionEvent(ResolveMessageEvent contact, bool broadcast) => new() {
            Body = "",
            Broadcast = broadcast,
            CampaignId = contact.Campaign.Id,
            Data = contact.Campaign.Data,
            MessageType = contact.Campaign.Type,
            RecipientId = contact.Contact.RecipientId,
            Title = ""
        };
    }
}
