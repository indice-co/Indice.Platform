using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    /// <summary>
    /// Template entity.
    /// </summary>
    public class DbTemplate
    {
        /// <summary>
        /// The unique id of the template.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The name of the template.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The content of the template.
        /// </summary>
        public CampaignContent Content { get; set; }
    }
}
