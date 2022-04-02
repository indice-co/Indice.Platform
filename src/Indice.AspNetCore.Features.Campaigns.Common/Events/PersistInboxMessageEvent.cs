using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when persisting a message in the store.
    /// </summary>
    public class PersistInboxMessageEvent
    {
        /// <summary>
        /// The id of the recipient.
        /// </summary>
        public string RecipientId { get; set; }
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The title of the message.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// Defines a CTA (call-to-action) text.
        /// </summary>
        public string ActionText { get; set; }
        /// <summary>
        /// Defines a CTA (call-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
        /// <summary>
        /// Specifies when a campaign was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool Published { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// Determines if campaign targets all user base.
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public ExpandoObject Data { get; set; }
        /// <summary>
        /// The distribution list of the campaign.
        /// </summary>
        public MessageDeliveryChannel DeliveryChannel { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public MessageType Type { get; set; }

        internal static PersistInboxMessageEvent FromInboxDistributionEvent(InboxDistributionEvent @event, string recipientId) => new() {
            ActionText = @event.ActionText,
            ActionUrl = @event.ActionUrl,
            ActivePeriod = @event.ActivePeriod,
            Body = @event.Content.Inbox.Body,
            CreatedAt = @event.CreatedAt,
            Data = @event.Data,
            DeliveryChannel = @event.DeliveryChannel,
            Id = @event.Id,
            IsGlobal = @event.IsGlobal,
            Published = @event.Published,
            RecipientId = recipientId,
            Title = @event.Content.Inbox.Title,
            Type = @event.Type
        };
    }
}
