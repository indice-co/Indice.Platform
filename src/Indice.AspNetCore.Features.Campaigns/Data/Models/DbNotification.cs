using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbNotification
    {
        public Guid Id { get; set; }
        public string UserCode { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset? ReadDate { get; set; }
        public DateTimeOffset? DeleteDate { get; set; }
        public Guid CampaignId { get; set; }
        public virtual DbCampaign Campaign { get; set; }
    }
}
