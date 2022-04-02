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
    /// <summary>
    /// An abstract class that serves as the base for implementing job handlers either local or on Azure functions.
    /// </summary>
    public abstract class CampaignJobHandlerBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignJobHandlerBase"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        public CampaignJobHandlerBase(IServiceProvider serviceProvider) {
            GetEventDispatcher = serviceProvider.GetRequiredService<Func<string, IEventDispatcher>>();
            GetPushNotificationService = serviceProvider.GetRequiredService<Func<string, IPushNotificationService>>();
            ContactResolver = serviceProvider.GetRequiredService<IContactResolver>();
            DistributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
            MessageService = serviceProvider.GetRequiredService<IMessageService>();
            ContactService = serviceProvider.GetRequiredService<IContactService>();
        }

        private Func<string, IEventDispatcher> GetEventDispatcher { get; }
        private Func<string, IPushNotificationService> GetPushNotificationService { get; }
        private IContactResolver ContactResolver { get; }
        private IDistributionListService DistributionListService { get; }
        private IMessageService MessageService { get; }
        private IContactService ContactService { get; }

        /// <summary>
        /// Distributes a campaign for further processing base on the <see cref="CampaignCreatedEvent.DeliveryChannel"/>.
        /// </summary>
        /// <param name="campaign">The event model used when a new campaign is created.</param>
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

        /// <summary>
        /// Creates events in order to distribute inbox messages to selected users or users from a distribution list.
        /// </summary>
        /// <param name="campaign">The event model used when distributing a message to selected users.</param>
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

        /// <summary>
        /// Persists a user message in the store.
        /// </summary>
        /// <param name="message">The event model used when persisting a message in the store.</param>
        protected virtual async Task PersistInboxMessage(PersistInboxMessageEvent message) {
            await MessageService.Create(new CreateMessageRequest {
                Body = message.Body,
                CampaignId = message.Id,
                RecipientId = message.RecipientId,
                Title = message.Title
            });
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: new ContactResolutionEvent(message.RecipientId),
                configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.ResolveContact)
            );
        }
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
                            RecipientId = userCode,
                            Campaign = campaign,
                            Broadcast = false
                        },
                        configure: options => options.WrapInEnvelope(false).At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow).WithQueueName(QueueNames.SendPushNotification));
                }
            }
        }

        /// <summary>
        /// Sends a push notification to all users or a single one.
        /// </summary>
        /// <param name="pushNotification">The event model used when sending a push notification.</param>
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
                await pushNotificationService.SendAsync(pushTitle, pushBody, data, pushNotification.RecipientId, classification: pushNotification.Campaign?.Type?.Name);
            }
        }
        #endregion

        #region Contacts Operations
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        protected virtual async Task ResolveContact(ContactResolutionEvent @event) {
            var contact = await ContactService.GetByRecipientId(@event.RecipientId);
            var isNew = contact is null;
            var needsUpdate = !isNew && DateTimeOffset.UtcNow - contact.UpdatedAt > TimeSpan.FromDays(1);
            if (isNew || needsUpdate) {
                contact = await ContactResolver.Resolve(@event.RecipientId.ToString());
                if (contact is not null) {
                    var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
                    await eventDispatcher.RaiseEventAsync(
                        payload: new UpsertContactEvent(@event.RecipientId, isNew, contact),
                        configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.UpsertContact)
                    );
                }
            }
        }

        /// <summary>
        /// Decides whether to create or update a contact in the system.
        /// </summary>
        /// <param name="event">The event model used when a contact is created or updated.</param>
        protected virtual async Task UpsertContact(UpsertContactEvent @event) {
            if (@event.IsNew) {
                await ContactService.Create(Mapper.ToCreateContactRequest(@event.Contact));
            } else {
                await ContactService.Update(@event.Contact.Id, Mapper.ToUpdateContactRequest(@event.Contact));
            }
        }
        #endregion
    }
}
