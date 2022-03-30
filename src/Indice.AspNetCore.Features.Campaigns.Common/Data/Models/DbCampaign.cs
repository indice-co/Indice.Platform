using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbCampaign
    {
        public Guid Id { get; set; } = Guid.NewGuid();
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
        public Guid? TypeId { get; set; }
        public Guid? AttachmentId { get; set; }
        public Guid? DistributionListId { get; set; }
        public virtual DbAttachment Attachment { get; set; }
        public virtual DbMessageType Type { get; set; }
        public virtual DbDistributionList DistributionList { get; set; }
    }
}
