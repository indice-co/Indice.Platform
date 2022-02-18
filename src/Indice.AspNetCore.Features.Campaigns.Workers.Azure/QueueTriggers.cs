using System.Dynamic;
using System.Text.Json;
using Indice.Events;
using Indice.Serialization;
using Indice.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers.Azure
{
    internal class QueueTriggers
    {
        public QueueTriggers(IEventDispatcher eventDispatcher, IPushNotificationService pushNotificationService) {
            EventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            PushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
        }

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
            if (!campaign.Published) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(CampaignQueueItem.CampaignDeliveryChannel.PushNotification)) {
                await ProcessPushNotifications(campaign);
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(CampaignQueueItem.CampaignDeliveryChannel.Email)) {
                // TODO: Create next hop to send campaign via email.
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(CampaignQueueItem.CampaignDeliveryChannel.SMS)) {
                // TODO: Create next hop to send campaign via SMS gateway.
                return;
            }
        }

        [Function(FunctionNames.SendPushNotification)]
        public async Task SendPushNotificationHandler(
            [QueueTrigger("%ENVIRONMENT%-" + QueueNames.SendPushNotification, Connection = "StorageConnection")] string message,
            FunctionContext executionContext
        ) {
            var logger = executionContext.GetLogger(FunctionNames.SendPushNotification);
            logger.LogInformation("Function '{FunctionName}' was triggered.", FunctionNames.SendPushNotification);
            var pushNotification = JsonSerializer.Deserialize<PushNotificationQueueItem>(message, JsonSerializerOptionDefaults.GetDefaultSettings());
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            var dataDictionary = data as IDictionary<string, object>;
            if (!dataDictionary.ContainsKey("id")) {
                data.TryAdd("id", pushNotification.Campaign.Id);
            }
            if (pushNotification.Broadcast) {
                await PushNotificationService.BroadcastAsync(pushNotification.Campaign.Title, data, pushNotification.Campaign?.Type?.Name);
            } else {
                await PushNotificationService.SendAsync(pushNotification.Campaign.Title, data, pushNotification.UserCode, classification: pushNotification.Campaign?.Type?.Name);
            }
        }

        private async Task ProcessPushNotifications(CampaignQueueItem campaign) {
            if (campaign.IsGlobal) {
                var globalMessage = new PushNotificationQueueItem {
                    Campaign = campaign,
                    Broadcast = true
                };
                await EventDispatcher.RaiseEventAsync(globalMessage, options => 
                    options.WrapInEnvelope(false)
                           .WithQueueName(QueueNames.SendPushNotification)
                );
            } else {
                foreach (var userCode in campaign.SelectedUserCodes) {
                    var userMessage = new PushNotificationQueueItem {
                        UserCode = userCode,
                        Campaign = campaign,
                        Broadcast = false
                    };
                    await EventDispatcher.RaiseEventAsync(userMessage, options =>
                        options.WrapInEnvelope(false)
                               .WithQueueName(QueueNames.SendPushNotification)
                    );
                }
            }
        }
    }
}
