using System;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbCampaign
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public Period ActivePeriod { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsNotification { get; set; }
        public Guid? AttachmentId { get; set; }
        public virtual DbCampaignAttachment Attachment { get; set; }
    }
}