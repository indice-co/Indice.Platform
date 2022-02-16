namespace Indice.Events
{
    /// <summary>
    /// Models a push notification when persisted as a queue item.
    /// </summary>
    public class PushNotificationQueueItem
    {
        /// <summary>
        /// Models a campaign when persisted as a queue item.
        /// </summary>
        public CampaignQueueItem Campaign { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Broadcast { get; set; }
        /// <summary>
        /// The 
        /// </summary>
        public string UserCode { get; set; }
    }
}
