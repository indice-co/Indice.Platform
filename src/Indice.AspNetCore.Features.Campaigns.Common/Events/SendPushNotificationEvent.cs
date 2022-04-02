namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when sending a push notification.
    /// </summary>
    public class SendPushNotificationEvent
    {
        /// <summary>
        /// The event model used when a new campaign is created.
        /// </summary>
        public CampaignCreatedEvent Campaign { get; set; }
        /// <summary>
        /// Defines if push notification is sent to all registered user devices.
        /// </summary>
        public bool Broadcast { get; set; }
        /// <summary>
        /// The id of the recipient.
        /// </summary>
        public string RecipientId { get; set; }
    }
}
