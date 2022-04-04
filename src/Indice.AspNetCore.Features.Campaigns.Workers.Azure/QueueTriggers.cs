using System.Text.Json;
using Indice.AspNetCore.Features.Campaigns.Events;
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

        [Function(QueueNames.CampaignCreated)]
        public async Task CampaignCreatedHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.CampaignCreated, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(QueueNames.CampaignCreated);
            logger.LogInformation("Function '{FunctionName}' was triggered.", QueueNames.CampaignCreated);
            var campaign = JsonSerializer.Deserialize<CampaignCreatedEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<CampaignCreatedEvent>();
            await handler.Process(campaign);
        }

        [Function(QueueNames.DistributeInbox)]
        public async Task InboxDistributionHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.DistributeInbox, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(QueueNames.DistributeInbox);
            logger.LogInformation("Function '{FunctionName}' was triggered.", QueueNames.DistributeInbox);
            var inboxDistribution = JsonSerializer.Deserialize<InboxDistributionEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<InboxDistributionEvent>();
            await handler.Process(inboxDistribution);
        }

        [Function(QueueNames.PersistInboxMessage)]
        public async Task PersistInboxMessageHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.PersistInboxMessage, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(QueueNames.PersistInboxMessage);
            logger.LogInformation("Function '{FunctionName}' was triggered.", QueueNames.PersistInboxMessage);
            var inboxMessage = JsonSerializer.Deserialize<PersistInboxMessageEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<PersistInboxMessageEvent>();
            await handler.Process(inboxMessage);
        }

        [Function(QueueNames.ContactResolution)]
        public async Task ResolveContactHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.ContactResolution, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(QueueNames.ContactResolution);
            logger.LogInformation("Function '{FunctionName}' was triggered.", QueueNames.ContactResolution);
            var @event = JsonSerializer.Deserialize<ContactResolutionEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<ContactResolutionEvent>();
            await handler.Process(@event);
        }

        [Function(QueueNames.UpsertContact)]
        public async Task UpsertContactHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.UpsertContact, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(QueueNames.UpsertContact);
            logger.LogInformation("Function '{FunctionName}' was triggered.", QueueNames.UpsertContact);
            var @event = JsonSerializer.Deserialize<UpsertContactEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<UpsertContactEvent>();
            await handler.Process(@event);
        }

        [Function(QueueNames.SendPushNotification)]
        public async Task SendPushNotificationHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.SendPushNotification, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(QueueNames.SendPushNotification);
            logger.LogInformation("Function '{FunctionName}' was triggered.", QueueNames.SendPushNotification);
            var pushNotification = JsonSerializer.Deserialize<SendPushNotificationEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<SendPushNotificationEvent>();
            await handler.Process(pushNotification);
        }
    }
}
