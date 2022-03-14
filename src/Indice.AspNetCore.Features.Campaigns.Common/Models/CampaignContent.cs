namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CampaignContent
    {
        /// <summary>
        /// 
        /// </summary>
        public MessageContent Inbox { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public MessageContent Push { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public MessageContent Email { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public MessageContent Sms { get; set; }
    }
}
