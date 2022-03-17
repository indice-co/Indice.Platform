using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    public abstract class CampaignJobHandlerBase
    {
        public CampaignJobHandlerBase(Func<string, IEventDispatcher> getEventDispatcher, Func<string, IPushNotificationService> getPushNotificationService) {
            EventDispatcherAccessor = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            PushNotificationServiceAccessor = getPushNotificationService ?? throw new ArgumentNullException(nameof(getPushNotificationService));
        }

        public Func<string, IEventDispatcher> EventDispatcherAccessor { get; }
        public Func<string, IPushNotificationService> PushNotificationServiceAccessor { get; }

        public virtual async Task DistributeCampaign(CampaignCreatedEvent campaign) {
            if (!campaign.Published) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.PushNotification)) {
                await ProcessPushNotifications(campaign);
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.Email)) {
                // TODO: Create next hop to send campaign via email.
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.SMS)) {
                // TODO: Create next hop to send campaign via SMS gateway.
                return;
            }
        }

        public virtual async Task ProcessPushNotifications(CampaignCreatedEvent campaign) {
            var eventDispatcher = EventDispatcherAccessor(KeyedServiceNames.EventDispatcherAzureServiceKey);
            if (campaign.IsGlobal) {
                var globalMessage = new SendPushNotificationEvent {
                    Campaign = campaign,
                    Broadcast = true
                };
                await eventDispatcher.RaiseEventAsync(globalMessage, options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification));
            } else {
                foreach (var userCode in campaign.SelectedUserCodes) {
                    var userMessage = new SendPushNotificationEvent {
                        UserCode = userCode,
                        Campaign = campaign,
                        Broadcast = false
                    };
                    await eventDispatcher.RaiseEventAsync(userMessage, options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification));
                }
            }
        }

        public virtual async Task DispatchPushNotification(SendPushNotificationEvent pushNotification) {
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            var dataDictionary = data as IDictionary<string, object>;
            if (!dataDictionary.ContainsKey("id")) {
                data.TryAdd("id", pushNotification.Campaign.Id);
            }
            var pushNotificationService = PushNotificationServiceAccessor(KeyedServiceNames.PushNotificationServiceAzureKey);
            var pushContent = pushNotification.Campaign.Content.Push;
            var pushTitle = pushContent?.Title ?? pushNotification.Campaign.Title;
            var pushBody = pushContent?.Body ?? "-";
            if (pushNotification.Broadcast) {
                await pushNotificationService.BroadcastAsync(pushTitle, pushBody, data, pushNotification.Campaign?.Type?.Name);
            } else {
                await pushNotificationService.SendAsync(pushTitle, pushBody, data, pushNotification.UserCode, classification: pushNotification.Campaign?.Type?.Name);
            }
        }
    }
}
