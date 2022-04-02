namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The campaign content for all delivery channels.
    /// </summary>
    public class CampaignContent
    {
        /// <summary>
        /// Campaign content for inbox.
        /// </summary>
        public MessageContent Inbox { get; set; }
        /// <summary>
        /// Campaign content for push notification.
        /// </summary>
        public MessageContent Push { get; set; }
        /// <summary>
        /// Campaign content for email.
        /// </summary>
        public MessageContent Email { get; set; }
        /// <summary>
        /// Campaign content for SMS.
        /// </summary>
        public MessageContent Sms { get; set; }
    }
}
