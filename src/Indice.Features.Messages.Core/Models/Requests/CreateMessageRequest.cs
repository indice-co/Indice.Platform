namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The event model used when creating a new inbox message.
    /// </summary>
    public class CreateMessageRequest
    {
        /// <summary>
        /// The id of the recipient.
        /// </summary>
        public string RecipientId { get; set; }
        /// <summary>
        /// The title of the message.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid CampaignId { get; set; }
    }
}
