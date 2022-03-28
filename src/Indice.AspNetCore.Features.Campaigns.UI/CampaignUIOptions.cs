using System;
using Indice.AspNetCore.EmbeddedUI;

namespace Indice.AspNetCore.Features.Campaigns.UI
{
    /// <summary>
    /// Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.
    /// </summary>
    public class CampaignUIOptions : SpaUIOptions
    {
        /// <summary>
        /// Controls the active delivery channels in the UI, when creating a new campaign.
        /// </summary>
        public CampaignDeliveryChannel ActiveDeliveryChannels { get; set; } = CampaignDeliveryChannel.All;
    }

    /// <summary>
    /// The delivery channel of a campaign.
    /// </summary>
    [Flags]
    public enum CampaignDeliveryChannel : byte
    {
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
        SMS = 8,
        /// <summary>
        /// All delivery channels.
        /// </summary>
        All = 16
    }
}
