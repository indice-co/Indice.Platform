using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbCampaignType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }
}
