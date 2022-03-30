using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    public class CampaignCreatedEvent
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public CampaignContent Content { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Published { get; set; }
        public Period ActivePeriod { get; set; }
        public bool IsGlobal { get; set; }
        public ExpandoObject Data { get; set; }
        public MessageDeliveryChannel DeliveryChannel { get; set; }
        public MessageType Type { get; set; }
        public DistributionList DistributionList { get; set; }
        public List<string> SelectedUserCodes { get; set; } = new List<string>();
    }
}
