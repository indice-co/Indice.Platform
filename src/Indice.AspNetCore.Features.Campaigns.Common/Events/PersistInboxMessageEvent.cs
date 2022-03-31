using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    public class PersistInboxMessageEvent
    {
        public string RecipientId { get; set; }
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Published { get; set; }
        public Period ActivePeriod { get; set; }
        public bool IsGlobal { get; set; }
        public ExpandoObject Data { get; set; }
        public MessageDeliveryChannel DeliveryChannel { get; set; }
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
