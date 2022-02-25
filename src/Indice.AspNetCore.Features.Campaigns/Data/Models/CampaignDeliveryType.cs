using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    /// <summary>
    /// The delivery channel of a campaign.
    /// </summary>
    [Flags]
    public enum CampaignDeliveryChannel : byte
    {
        /// <summary>
        /// No delivery.
        /// </summary>
        None = 0,
        /// <summary>
        /// Campaign is displayed on user inbox.
        /// </summary>
        Inbox = 1,
        /// <summary>
        /// Campaign is sent as push notification.
        /// </summary>
        PushNotification = 2,
        /// <summary>
        /// Campaign is sent as email.
        /// </summary>
        Email = 4,
        /// <summary>
        /// Campaign is sent as SMS.
        /// </summary>
        SMS = 8
    }
}
