using System;
using System.Collections.Generic;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbCampaign
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public Dictionary<MessageDeliveryChannel, MessageContent> Content { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Published { get; set; }
        public Period ActivePeriod { get; set; }
        public bool IsGlobal { get; set; }
        public dynamic Data { get; set; }
        public MessageDeliveryChannel DeliveryChannel { get; set; }
        public Guid? TypeId { get; set; }
        public Guid? AttachmentId { get; set; }
        public Guid? DistributionListId { get; set; }
        public virtual DbAttachment Attachment { get; set; }
        public virtual DbMessageType Type { get; set; }
        public virtual DbDistributionList DistributionList { get; set; }
    }
}
