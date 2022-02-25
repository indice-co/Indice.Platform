namespace Indice.AspNetCore.Features.Campaigns.Workers.Azure
{
    internal class QueueNames
    {
        public const string CampaignCreated = "campaign-created";
        public const string SendPushNotification = "campaign-send-push-notification";
    }

    internal class FunctionNames
    {
        public const string CampaignCreated = nameof(CampaignCreated);
        public const string SendPushNotification = nameof(SendPushNotification);
    }

    internal class KeyedServiceNames
    {
        public const string FileServiceKey = "Campaigns:FileServiceKey";
    }
}
