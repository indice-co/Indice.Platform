using System.Text.Json;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers.Azure
{
    internal class QueueTriggers
    {
        public QueueTriggers(CampaignJobHandlerFactory campaignJobHandlerFactory) {
            CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        }

        private CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        [Function(EventNames.CampaignPublished)]
        public async Task CampaignPublishedHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.CampaignPublished, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(EventNames.CampaignPublished);
            logger.LogInformation("Function '{FunctionName}' was triggered.", EventNames.CampaignPublished);
            var campaign = JsonSerializer.Deserialize<CampaignPublishedEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<CampaignPublishedEvent>();
            await handler.Process(campaign);
        }

        [Function(EventNames.ResolveMessage)]
        public async Task ResolveMessageHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.ResolveMessage, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(EventNames.ResolveMessage);
            logger.LogInformation("Function '{FunctionName}' was triggered.", EventNames.ResolveMessage);
            var @event = JsonSerializer.Deserialize<ResolveMessageEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<ResolveMessageEvent>();
            await handler.Process(@event);
        }

        [Function(EventNames.SendPushNotification)]
        public async Task SendPushNotificationHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.SendPushNotification, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(EventNames.SendPushNotification);
            logger.LogInformation("Function '{FunctionName}' was triggered.", EventNames.SendPushNotification);
            var pushNotification = JsonSerializer.Deserialize<SendPushNotificationEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<SendPushNotificationEvent>();
            await handler.Process(pushNotification);
        }
    }
}
