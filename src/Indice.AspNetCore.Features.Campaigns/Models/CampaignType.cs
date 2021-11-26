using System;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Models a campaign type.
    /// </summary>
    public  class CampaignType
    {
        /// <summary>
        /// The id of a campaign type.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The name of a campaign type.
        /// </summary>
        public string Name { get; set; }
    }
}
