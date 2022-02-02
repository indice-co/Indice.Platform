namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class PushNotificationQueueItem
    {
        public CampaignQueueItem Campaign { get; set; }
        public bool Broadcast { get; set; }
        public string UserCode { get; set; }
    }
}
