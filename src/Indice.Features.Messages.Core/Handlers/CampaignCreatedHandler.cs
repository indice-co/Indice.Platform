using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="CampaignCreatedEvent"/>.</summary>
public sealed class CampaignCreatedHandler : ICampaignJobHandler<CampaignCreatedEvent>
{
    /// <summary>Creates a new instance of <see cref="CampaignCreatedHandler"/>.</summary>
    /// <param name="eventDispatcherFactory">Provides methods that allow application components to communicate with each other by dispatching events.</param>
    /// <param name="contactService">A service that contains contact related operations.</param>
    /// <param name="campaignService">A service that contains campaign related operations.</param>
    /// <param name="inboxNotifier">Notifies users in real time when a new global inbox message is published. Optional.</param>
    /// <param name="logger">A logger. Optional.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignCreatedHandler(
        IEventDispatcherFactory eventDispatcherFactory,
        IContactService contactService,
        ICampaignService campaignService,
        IInboxNotifier? inboxNotifier = null,
        ILogger<CampaignCreatedHandler>? logger = null
    ) {
        EventDispatcherFactory = eventDispatcherFactory ?? throw new ArgumentNullException(nameof(eventDispatcherFactory));
        ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        InboxNotifier = inboxNotifier ?? new InboxNotifierNoop();
        Logger = logger ?? NullLogger<CampaignCreatedHandler>.Instance;
    }

    private IEventDispatcherFactory EventDispatcherFactory { get; }
    private IContactService ContactService { get; }
    private ICampaignService CampaignService { get; }
    private IInboxNotifier InboxNotifier { get; }
    private ILogger<CampaignCreatedHandler> Logger { get; }

    /// <summary>Distributes a campaign for further processing base on the <see cref="CampaignCreatedEvent.MessageChannelKind"/>.</summary>
    /// <param name="campaign">The event model used when a new campaign is created.</param>
    public async Task Process(CampaignCreatedEvent campaign) {
        // If campaign is global and has push notification as delivery channel, then we short-circuit the flow and we immediately broadcast the message.
        if (campaign.IsGlobal && campaign.MessageChannelKind.HasFlag(MessageChannelKind.PushNotification)) {
            var eventDispatcher = EventDispatcherFactory.Create(KeyedServiceNames.EventDispatcherServiceKey);
            await eventDispatcher.RaiseEventAsync(
                payload: SendPushNotificationEvent.FromCampaignCreatedEvent(campaign, broadcast: true),
                configure: builder =>
                    builder.WrapInEnvelope()
                           .At(campaign.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow)
                           .WithQueueName(EventNames.SendPushNotification)
                );
        }
        // If campaign is global, published and has inbox as delivery channel, then we notify all connected users in real time.
        if (campaign.IsGlobal && campaign.Published && campaign.MessageChannelKind.HasFlag(MessageChannelKind.Inbox)
            && InboxMessageNotification.IsActive(campaign.ActivePeriod, DateTimeOffset.UtcNow)) {
            try {
                await InboxNotifier.NotifyAllAsync(InboxMessageNotification.FromCampaign(campaign));
            } catch (Exception exception) {
                // A failed real time notification should never fail (and retry) the campaign distribution.
                Logger.LogWarning(exception, "Failed to send the real time inbox notification for global campaign {CampaignId}.", campaign.Id);
            }
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
            var eventDispatcher = EventDispatcherFactory.Create(KeyedServiceNames.EventDispatcherServiceKey);
            foreach (var contact in contacts) {
                await eventDispatcher.RaiseEventAsync(ResolveMessageEvent.FromCampaignCreatedEvent(campaign, contact), builder => builder.WrapInEnvelope().WithQueueName(EventNames.ResolveMessage));
            }
            if (campaign.Published) {
                await CampaignService.SetRecipientCount(campaign.Id, contacts.Count);
            }
        }
    }
}
