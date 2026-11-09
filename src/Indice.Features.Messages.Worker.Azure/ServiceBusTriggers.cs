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
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Worker.Azure;
internal class ServiceBusTriggers
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

    public ServiceBusTriggers(
        MessageJobHandlerFactory campaignJobHandlerFactory,
        IEventDispatcherFactory eventDispatcherFactory,
        IOptions<EventDispatcherAzureServiceBusOptions> options
    ) {
        CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        EventDispatcherFactory = eventDispatcherFactory ?? throw new ArgumentNullException(nameof(eventDispatcherFactory));
        _options = options.Value;
    }
    private readonly EventDispatcherAzureServiceBusOptions _options;
    private MessageJobHandlerFactory CampaignJobHandlerFactory { get; }
    private IEventDispatcherFactory EventDispatcherFactory { get; }
    public const string ServiceBusTriggerPrefix = "servicebus-";

    [Function(ServiceBusTriggerPrefix + EventNames.CampaignCreated)]
    public async Task CampaignPublishedHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.CampaignCreated, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.CampaignCreated);
        var envelope = JsonSerializer.Deserialize<Envelope<CampaignCreatedEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
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

    [Function(ServiceBusTriggerPrefix + EventNames.ResolveMessage)]
    public async Task ResolveMessageHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.ResolveMessage, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.ResolveMessage);
        var envelope = JsonSerializer.Deserialize<Envelope<ResolveMessageEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<ResolveMessageEvent>().Process(payload!);
    }

    [Function(ServiceBusTriggerPrefix + EventNames.SendPushNotification)]
    public async Task SendPushNotificationHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.SendPushNotification, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.SendPushNotification);
        var envelope = JsonSerializer.Deserialize<Envelope<SendPushNotificationEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<SendPushNotificationEvent>().Process(payload!);
    }

    [Function(ServiceBusTriggerPrefix + EventNames.SendEmail)]
    public async Task SendEmailHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.SendEmail, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.SendEmail);
        var envelope = JsonSerializer.Deserialize<Envelope<SendEmailEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<SendEmailEvent>().Process(payload!);
    }

    [Function(ServiceBusTriggerPrefix + EventNames.SendSms)]
    public async Task SendSmsHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.SendSms, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.SendSms);
        var envelope = JsonSerializer.Deserialize<Envelope<SendSmsEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<SendSmsEvent>().Process(payload!);
    }

    [Function(ServiceBusTriggerPrefix + EventNames.MarkAllAsRead)]
    public async Task MarkAllAsReadHandler(
        [QueueTrigger("%ENVIRONMENT%-" + EventNames.MarkAllAsRead, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.MarkAllAsRead);
        var envelope = JsonSerializer.Deserialize<Envelope<MarkMessagesReadEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<MarkMessagesReadEvent>().Process(payload!);
    }

    [Function(ServiceBusTriggerPrefix + EventNames.MarkAllAsUnread)]
    public async Task MarkAllAsUnreadHandler(
        [QueueTrigger("%ENVIRONMENT%-" + EventNames.MarkAllAsUnread, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.MarkAllAsUnread);
        var envelope = JsonSerializer.Deserialize<Envelope<MarkMessagesUnreadEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<MarkMessagesUnreadEvent>().Process(payload!);
    }

    [Function(ServiceBusTriggerPrefix + EventNames.MergeContacts)]
    public async Task MergeContactsHandler(
        [ServiceBusTrigger("%ENVIRONMENT%-" + EventNames.MergeContacts, Connection = "ServiceBusConnection")] byte[] message,
        FunctionContext functionContext
    ) {
        LogExecution(functionContext, EventNames.MergeContacts);
        var envelope = JsonSerializer.Deserialize<Envelope<MergeContactsEvent>>(await ReadMessageAsync(message), JsonSerializerOptions)!;
        var payload = envelope.Payload;
        await CampaignJobHandlerFactory.CreateFor<MergeContactsEvent>().Process(payload!);
    }

    private async Task<byte[]> ReadMessageAsync(byte[] message) {
        if (_options.UseCompression) {
            return await CompressionUtils.Decompress(message);
        }
        return message;
    }
    private static void LogExecution(FunctionContext functionContext, string eventName) {
        var logger = functionContext.GetLogger(ServiceBusTriggerPrefix + eventName);
        logger.LogInformation("Function '{FunctionName}' was triggered.", ServiceBusTriggerPrefix + eventName);
    }
}
