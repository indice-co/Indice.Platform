using System;
using System.Dynamic;
using System.Text.Json;
using Indice.Serialization;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbCampaign
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Content { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Published { get; set; }
        public Period ActivePeriod { get; set; }
        public bool IsGlobal { get; set; }
        public ExpandoObject Data { get; set; }
        public string DataJson {
            get { return Data != null ? JsonSerializer.Serialize(Data, JsonSerializerOptionDefaults.GetDefaultSettings()) : null; }
            set { Data = value != null ? JsonSerializer.Deserialize<ExpandoObject>(value, JsonSerializerOptionDefaults.GetDefaultSettings()) : null; }
        }
        public CampaignDeliveryChannel DeliveryChannel { get; set; }
        public Guid? TypeId { get; set; }
        public Guid? AttachmentId { get; set; }
        public virtual DbAttachment Attachment { get; set; }
        public virtual DbCampaignType Type { get; set; }
    }
}
