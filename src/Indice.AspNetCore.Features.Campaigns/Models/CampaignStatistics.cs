using System;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CampaignStatistics
    {
        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ReadCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? NotReadCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int DeletedCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ClickToActionCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}
