using System.Dynamic;
using System.Text.Json;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    public abstract class CampaignJobHandlerBase
    {
        public CampaignJobHandlerBase(
            Func<string, IEventDispatcher> getEventDispatcher,
            Func<string, IPushNotificationService> getPushNotificationService
        ) {
            GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
            GetPushNotificationService = getPushNotificationService ?? throw new ArgumentNullException(nameof(getPushNotificationService));
        }

        public Func<string, IEventDispatcher> GetEventDispatcher { get; }
        public Func<string, IPushNotificationService> GetPushNotificationService { get; }

        protected virtual async Task TryDistributeCampaign(CampaignCreatedEvent campaign) {
            // If campaign is not published, then nothing is sent yet.
            if (!campaign.Published) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.Inbox)) {
                await ProcessInbox(campaign);
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.PushNotification)) {
                await ProcessPushNotifications(campaign);
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.Email)) {
                return;
            }
            if (campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.SMS)) {
                return;
            }
        }

        protected virtual async Task DistributeInbox(InboxDistributionEvent campaign) {
            var recipients = campaign.SelectedUserCodes ?? new List<string>();
            if (campaign.DistributionList is not null) {
                await Task.CompletedTask;
            }
        }

        protected virtual async Task DispatchPushNotification(SendPushNotificationEvent pushNotification) {
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            data.TryAdd("campaignId", pushNotification.Campaign.Id);
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

        private async Task ProcessInbox(CampaignCreatedEvent campaign) {
            // If campaign is intended for all users, then inbox messages are created upon user interaction (i.e. read a message). 
            if (campaign.IsGlobal) {
                return;
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherAzureServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: InboxDistributionEvent.FromCampaignCreatedEvent(campaign),
                configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.DistributeInbox)
            );
        }

        private async Task ProcessPushNotifications(CampaignCreatedEvent campaign) {
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherAzureServiceKey);
            // If campaign is intended for all users, then we can broadcast the notification to everyone at once.
            if (campaign.IsGlobal) {
                await eventDispatcher.RaiseEventAsync(
                    payload: new SendPushNotificationEvent {
                        Campaign = campaign,
                        Broadcast = true
                    },
                    configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification)
                );
            } else {
                foreach (var userCode in campaign.SelectedUserCodes) {
                    await eventDispatcher.RaiseEventAsync(
                        payload: new SendPushNotificationEvent {
                            UserCode = userCode,
                            Campaign = campaign,
                            Broadcast = false
                        },
                        configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification));
                }
            }
        }
    }
}
