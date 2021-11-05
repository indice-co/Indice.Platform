namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CampaignDetails : Campaign
    {
        /// <summary>
        /// 
        /// </summary>
        public AttachmentLink Image { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int MessagesCount { get; set; }
    }
}