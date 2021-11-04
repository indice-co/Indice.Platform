using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class UpdateCampaignRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ActionText { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; }
    }
}
