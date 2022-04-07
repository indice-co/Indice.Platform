using System.Text.Json;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Azure
{
    internal class QueueTriggers
    {
        public QueueTriggers(MessageJobHandlerFactory campaignJobHandlerFactory) {
            CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        }

        private MessageJobHandlerFactory CampaignJobHandlerFactory { get; }

        [Function(EventNames.CampaignPublished)]
        public async Task CampaignPublishedHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.CampaignPublished, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) => await Handle<CampaignPublishedEvent>(executionContext, EventNames.CampaignPublished, message);

        [Function(EventNames.ResolveMessage)]
        public async Task ResolveMessageHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.ResolveMessage, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) => await Handle<ResolveMessageEvent>(executionContext, EventNames.ResolveMessage, message);

        [Function(EventNames.SendPushNotification)]
        public async Task SendPushNotificationHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.SendPushNotification, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) => await Handle<SendPushNotificationEvent>(executionContext, EventNames.SendPushNotification, message);

        [Function(EventNames.SendEmail)]
        public async Task SendEmailHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.SendEmail, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) => await Handle<SendEmailEvent>(executionContext, EventNames.SendEmail, message);

        [Function(EventNames.SendSms)]
        public async Task SendSmsHandler(
            [QueueTrigger("%ENVIRONMENT%-" + EventNames.SendSms, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) => await Handle<SendSmsEvent>(executionContext, EventNames.SendSms, message);

        private async Task Handle<TEvent>(FunctionContext executionContext, string eventName, string message) where TEvent : class {
            var logger = executionContext.GetLogger(eventName);
            logger.LogInformation("Function '{FunctionName}' was triggered.", eventName);
            var @event = JsonSerializer.Deserialize<TEvent>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var handler = CampaignJobHandlerFactory.Create<TEvent>();
            await handler.Process(@event);
        }
    }
}
