using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateCampaignRequest : UpdateCampaignRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsNotification { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Guid? AttachmentId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> SelectedUserCodes { get; set; } = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        public string ActionUrl { get; set; }
    }
}
