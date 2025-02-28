using System.IO.Compression;
using System.Text.Json;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core;
using Indice.Serialization;
using Indice.Services;
using Indice.Types;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Azure;

internal class ServiceBusTriggers
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

    public ServiceBusTriggers(
        MessageJobHandlerFactory campaignJobHandlerFactory,
        IEventDispatcherFactory eventDispatcherFactory
    ) {
        CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        EventDispatcherFactory = eventDispatcherFactory ?? throw new ArgumentNullException(nameof(eventDispatcherFactory));
    }

    private MessageJobHandlerFactory CampaignJobHandlerFactory { get; }
    private IEventDispatcherFactory EventDispatcherFactory { get; }

    [Function("SB-"+EventNames.CampaignCreated)]
    public async Task CampaignPublishedHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.CampaignCreated, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.CampaignCreated);
        //var originalMessage = await CompressionUtils.Decompress(message);
        var envelope = JsonSerializer.Deserialize<Envelope<CampaignCreatedEvent>>(message, JsonSerializerOptions)!;
        var payload = envelope.Payload!;
        var campaignStart = payload.ActivePeriod?.From;
        // Azure queues can store a queue message with a visibility window up to 7 days. So if a campaign must start (appear on queue) after more than 7 days then we should check the campaign start date and re-enqueue the message.
        if (campaignStart > DateTimeOffset.UtcNow) {
            var nextExecutionTimeSpan = campaignStart.Value - DateTimeOffset.UtcNow;
            var visibilityWindow = nextExecutionTimeSpan > TimeSpan.FromDays(5) ? TimeSpan.FromDays(5) : nextExecutionTimeSpan;
            var eventDispatcher = EventDispatcherFactory.Create(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(envelope, options => options.WrapInEnvelope(false).Delay(visibilityWindow).WithQueueName(EventNames.CampaignCreated));
            return;
        }
        await CampaignJobHandlerFactory.CreateFor<CampaignCreatedEvent>().Process(payload);
    }

    [Function("SB-"+EventNames.ResolveMessage)]
    public async Task ResolveMessageHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.ResolveMessage, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.ResolveMessage);
        //var originalMessage = await CompressionUtils.Decompress(message);
        var envelope = JsonSerializer.Deserialize<Envelope<ResolveMessageEvent>>(message, JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<ResolveMessageEvent>().Process(payload!);
    }

    [Function("SB-"+EventNames.SendPushNotification)]
    public async Task SendPushNotificationHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.SendPushNotification, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.SendPushNotification);
        //var originalMessage = await CompressionUtils.Decompress(message);
        var envelope = JsonSerializer.Deserialize<Envelope<SendPushNotificationEvent>>(message, JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<SendPushNotificationEvent>().Process(payload!);
    }

    [Function("SB-"+EventNames.SendEmail)]
    public async Task SendEmailHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.SendEmail, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.SendEmail);
        //var originalMessage = await CompressionUtils.Decompress(message);
        var envelope = JsonSerializer.Deserialize<Envelope<SendEmailEvent>>(message, JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<SendEmailEvent>().Process(payload!);
    }

    [Function("SB-"+EventNames.SendSms)]
    public async Task SendSmsHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.SendSms, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.SendSms);
        //var originalMessage = await CompressionUtils.Decompress(message);
        var envelope = JsonSerializer.Deserialize<Envelope<SendSmsEvent>>(message, JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<SendSmsEvent>().Process(payload!);
    }

    private static void LogExecution(FunctionContext functionContext, string eventName) {
        var logger = functionContext.GetLogger(eventName);
        logger.LogInformation("Function '{FunctionName}' was triggered.", eventName);
    }
}