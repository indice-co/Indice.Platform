using System.Dynamic;
using System.Text.Json;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns
{
    public abstract class CampaignJobHandlerBase
    {
        public CampaignJobHandlerBase(IServiceProvider serviceProvider) {
            GetEventDispatcher = serviceProvider.GetRequiredService<Func<string, IEventDispatcher>>();
            GetPushNotificationService = serviceProvider.GetRequiredService<Func<string, IPushNotificationService>>();
            ContactResolver = serviceProvider.GetRequiredService<IContactResolver>();
            DistributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
            MessageService = serviceProvider.GetRequiredService<IMessageService>();
        }

        public Func<string, IEventDispatcher> GetEventDispatcher { get; }
        public Func<string, IPushNotificationService> GetPushNotificationService { get; }
        public IContactResolver ContactResolver { get; }
        public IDistributionListService DistributionListService { get; }
        public IMessageService MessageService { get; }

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

        #region Inbox Operations
        private async Task ProcessInbox(CampaignCreatedEvent campaign) {
            // If campaign is intended for all users, then inbox messages are created upon user interaction (i.e. read a message). 
            if (campaign.IsGlobal) {
                return;
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: InboxDistributionEvent.FromCampaignCreatedEvent(campaign),
                configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.DistributeInbox)
            );
        }

        protected virtual async Task DistributeInbox(InboxDistributionEvent campaign) {
            var recipients = campaign.SelectedUserCodes ?? new List<string>();
            if (campaign.DistributionList is not null) {
                var contacts = await DistributionListService.GetContactsList(campaign.DistributionList.Id, new ListOptions { 
                    Page = 1,
                    Size = int.MaxValue
                });
                recipients.AddRange(contacts.Items.Select(x => x.RecipientId));
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            foreach (var id in recipients) {
                await eventDispatcher.RaiseEventAsync(
                    payload: PersistInboxMessageEvent.FromInboxDistributionEvent(campaign, id),
                    configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.PersistInboxMessage)
                );
            }
        }

        protected virtual async Task PersistInboxMessage(PersistInboxMessageEvent message) => await MessageService.Create(new CreateMessageRequest {
            Body = message.Body,
            CampaignId = message.Id,
            Id = message.Id,
            RecipientId = message.RecipientId,
            Title = message.Title
        });
        #endregion

        #region Push Notifications Operations
        private async Task ProcessPushNotifications(CampaignCreatedEvent campaign) {
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
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

        protected virtual async Task DispatchPushNotification(SendPushNotificationEvent pushNotification) {
            var data = pushNotification.Campaign?.Data ?? new ExpandoObject();
            data.TryAdd("campaignId", pushNotification.Campaign.Id);
            var pushNotificationService = GetPushNotificationService(KeyedServiceNames.PushNotificationServiceKey);
            var pushContent = pushNotification.Campaign.Content.Push;
            var pushTitle = pushContent?.Title ?? pushNotification.Campaign.Title;
            var pushBody = pushContent?.Body ?? "-";
            if (pushNotification.Broadcast) {
                await pushNotificationService.BroadcastAsync(pushTitle, pushBody, data, pushNotification.Campaign?.Type?.Name);
            } else {
                await pushNotificationService.SendAsync(pushTitle, pushBody, data, pushNotification.UserCode, classification: pushNotification.Campaign?.Type?.Name);
            }
        }
        #endregion
    }
}
