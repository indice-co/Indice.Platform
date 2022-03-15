using System.Text.Json;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.Serialization;
using Indice.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers.Azure
{
    internal class QueueTriggers : CampaignJobHandlerBase
    {
        public QueueTriggers(
            Func<string, IEventDispatcher> getEventDispatcher,
            Func<string, IPushNotificationService> getPushNotificationService
        ) : base(getEventDispatcher, getPushNotificationService) { }

        public IEventDispatcher EventDispatcher { get; }
        public IPushNotificationService PushNotificationService { get; }

        [Function(FunctionNames.CampaignCreated)]
        public async Task CampaignCreatedHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.CampaignCreated, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(FunctionNames.CampaignCreated);
            logger.LogInformation("Function '{FunctionName}' was triggered.", FunctionNames.CampaignCreated);
            var campaign = JsonSerializer.Deserialize<CampaignQueueItem>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            await base.DistributeCampaign(campaign);
        }

        [Function(FunctionNames.SendPushNotification)]
        public async Task SendPushNotificationHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.SendPushNotification, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(FunctionNames.SendPushNotification);
            logger.LogInformation("Function '{FunctionName}' was triggered.", FunctionNames.SendPushNotification);
            var pushNotification = JsonSerializer.Deserialize<PushNotificationQueueItem>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            await DispatchPushNotification(pushNotification);
        }
    }
}
