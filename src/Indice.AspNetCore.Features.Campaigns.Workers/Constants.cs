namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class QueueNames
    {
        public const string CampaignCreated = "campaign-created";
        public const string SendPushNotification = "send-push-notification";
    }

    internal class KeyedServiceNames 
    {
        internal const string PushNotificationServiceAzureKey = "Campaigns:PushNotificationServiceAzureKey";
    }
}
