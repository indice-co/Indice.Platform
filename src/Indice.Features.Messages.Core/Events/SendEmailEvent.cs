using System.Dynamic;
using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Events
{
    /// <summary>
    /// The event model used when sending an email.
    /// </summary>
    public class SendEmailEvent
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
        /// Creates a <see cref="SendEmailEvent"/> instance from a <see cref="ResolveMessageEvent"/> instance.
        /// </summary>
        /// <param name="contact">The event model used when a contact is resolved from an external system.</param>
        /// <param name="broadcast">Defines if push notification is sent to all registered user devices.</param>
        public static SendPushNotificationEvent FromContactResolutionEvent(ResolveMessageEvent contact, bool broadcast) => new() {
            Body = contact.Campaign.Content["email"].Body,
            Broadcast = broadcast,
            CampaignId = contact.Campaign.Id,
            Data = contact.Campaign.Data,
            MessageType = contact.Campaign.Type,
            RecipientId = contact.Contact.RecipientId,
            Title = contact.Campaign.Content["email"].Title
        };
    }
}
