using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbHit
    {
        public int Id { get; }
        public Guid CampaignId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
