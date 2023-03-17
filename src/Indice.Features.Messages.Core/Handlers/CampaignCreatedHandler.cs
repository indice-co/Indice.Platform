using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="CampaignCreatedEvent"/>.</summary>
public sealed class CampaignCreatedHandler : ICampaignJobHandler<CampaignCreatedEvent>
{
    /// <summary>Creates a new instance of <see cref="CampaignCreatedHandler"/>.</summary>
    /// <param name="getEventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
    /// <param name="contactService">A service that contains contact related operations.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignCreatedHandler(
        Func<string, IEventDispatcher> getEventDispatcher,
        IContactService contactService
    ) {
        GetEventDispatcher = getEventDispatcher ?? throw new ArgumentNullException(nameof(getEventDispatcher));
        ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
    }

    private Func<string, IEventDispatcher> GetEventDispatcher { get; }
    private IContactService ContactService { get; }

    /// <summary>Distributes a campaign for further processing base on the <see cref="CampaignCreatedEvent.MessageChannelKind"/>.</summary>
    /// <param name="campaign">The event model used when a new campaign is created.</param>
    public async Task Process(CampaignCreatedEvent campaign) {
        // If campaign is global and has push notification as delivery channel, then we short-circuit the flow and we immediately broadcast the message.
        if (campaign.IsGlobal && campaign.MessageChannelKind.HasFlag(MessageChannelKind.PushNotification)) {
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: SendPushNotificationEvent.FromCampaignCreatedEvent(campaign, broadcast: true),
                configure: builder =>
                    builder.WrapInEnvelope()
                           .At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow)
                           .WithQueueName(EventNames.SendPushNotification)
                );
        }
        // If campaign is not global and a distribution list has been set, then we will create multiple events in order to resolve contact info, merge campaign template with contact data and dispatch messages in various channels.
        if (!campaign.IsGlobal) {
            var contacts = new List<Contact>();
            if (campaign.DistributionListId.HasValue) {
                var contactsResultSet = await ContactService.GetList(new ListOptions<ContactListFilter> {
                    Page = 1,
                    Size = int.MaxValue,
                    Filter = new ContactListFilter { DistributionListId = campaign.DistributionListId.Value }
                });
                contacts.AddRange(contactsResultSet.Items);
            }
            var eventDispatcher = GetEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey);
            foreach (var contact in contacts) {
                await eventDispatcher.RaiseEventAsync(ResolveMessageEvent.FromCampaignCreatedEvent(campaign, contact), builder => builder.WrapInEnvelope().WithQueueName(EventNames.ResolveMessage));
            }
        }
    }
}
