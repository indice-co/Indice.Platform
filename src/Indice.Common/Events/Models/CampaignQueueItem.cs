using System;
using System.Collections.Generic;
using System.Dynamic;
using Indice.Types;

namespace Indice.Events
{
    /// <summary>
    /// Models a campaign when persisted as a queue item.
    /// </summary>
    public class CampaignQueueItem
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
        /// Defines a CTA (call-to-action) text.
        /// </summary>
        public string ActionText { get; set; }
        /// <summary>
        /// Defines a CTA (call-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
        /// <summary>
        /// Specifies when a campaign was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool Published { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// Determines if campaign targets all user base.
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public CampaignType Type { get; set; }
        /// <summary>
        /// The delivery channel of a campaign.
        /// </summary>
        public CampaignDeliveryChannel DeliveryChannel { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public ExpandoObject Data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> SelectedUserCodes { get; set; } = new List<string>();

        /// <summary>
        /// Models a campaign type.
        /// </summary>
        public class CampaignType
        {
            /// <summary>
            /// The id of a campaign type.
            /// </summary>
            public Guid Id { get; set; }
            /// <summary>
            /// The name of a campaign type.
            /// </summary>
            public string Name { get; set; }
        }

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
}
