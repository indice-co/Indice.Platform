using System;
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
        public bool IsActive { get; set; }
        public Period ActivePeriod { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsNotification { get; set; }
        public Guid? AttachmentId { get; set; }
        public virtual DbAttachment Attachment { get; set; }
    }
}