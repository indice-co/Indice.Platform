namespace Indice.AspNetCore.Features.Campaigns.Events
{
    public class SendPushNotificationEvent
    {
        public CampaignCreatedEvent Campaign { get; set; }
        public bool Broadcast { get; set; }
        public string UserCode { get; set; }
    }
}
