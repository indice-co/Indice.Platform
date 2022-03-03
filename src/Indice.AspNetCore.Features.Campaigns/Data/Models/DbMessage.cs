using System;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbMessage
    {
        public Guid Id { get; set; }
        public string RecipientId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTimeOffset? ReadDate { get; set; }
        public DateTimeOffset? DeleteDate { get; set; }
        public Guid CampaignId { get; set; }
        public virtual DbCampaign Campaign { get; set; }
    }
}
