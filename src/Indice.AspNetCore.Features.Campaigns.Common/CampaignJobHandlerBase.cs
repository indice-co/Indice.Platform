using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    public abstract class CampaignJobHandlerBase
    {
        public CampaignJobHandlerBase(Func<string, IEventDispatcher> getEventDispatcher, Func<string, IPushNotificationService> getPushNotificationService) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            GetPushNotificationService = getPushNotificationService ?? throw new ArgumentNullException(nameof(getPushNotificationService));
        }

        public Func<string, IEventDispatcher> GetEventDispatcher { get; }
        public Func<string, IPushNotificationService> GetPushNotificationService { get; }

        public virtual async Task TryDistributeCampaign(CampaignCreatedEvent campaign) {
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
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherAzureServiceKey);
            if (campaign.IsGlobal) {
                await eventDispatcher.RaiseEventAsync(
                    payload: new SendPushNotificationEvent {
                        Campaign = campaign,
                        Broadcast = true
                    }, 
                    configure: options => options.WrapInEnvelope(false)
                                                 .At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow)
                                                 .WithQueueName(QueueNames.SendPushNotification)
                );
            } else {
                foreach (var userCode in campaign.SelectedUserCodes) {
                    await eventDispatcher.RaiseEventAsync(
                        payload: new SendPushNotificationEvent {
                            UserCode = userCode,
                            Campaign = campaign,
                            Broadcast = false
                        },
                        configure: options => options.WrapInEnvelope(false)
                                                     .At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow)
                                                     .WithQueueName(QueueNames.SendPushNotification));
                }
            }
        }

        public virtual async Task DispatchPushNotification(SendPushNotificationEvent pushNotification) {
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            var dataDictionary = data as IDictionary<string, object>;
            if (!dataDictionary.ContainsKey("id")) {
                data.TryAdd("id", pushNotification.Campaign.Id);
            }
            var pushNotificationService = GetPushNotificationService(KeyedServiceNames.PushNotificationServiceAzureKey);
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
