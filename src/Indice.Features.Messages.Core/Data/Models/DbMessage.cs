namespace Indice.Features.Messages.Core.Data.Models
{
    /// <summary>
    /// Inbox message entity.
    /// </summary>
    public class DbMessage
    {
        /// <summary>
        /// The unique identifier of the user message.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The id of the recipient.
        /// </summary>
        public string RecipientId { get; set; }
        /// <summary>
        /// Determines if a message is deleted by the user.
        /// </summary>
        public bool IsDeleted { get; set; }
        /// <summary>
        /// Determines if a message is read by the user.
        /// </summary>
        public bool IsRead { get; set; }
        /// <summary>
        /// The title of the inbox message.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body of the inbox message.
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// Defines when the inbox message was read.
        /// </summary>
        public DateTimeOffset? ReadDate { get; set; }
        /// <summary>
        /// Defines when the inbox message was deleted.
        /// </summary>
        public DateTimeOffset? DeleteDate { get; set; }
        /// <summary>
        /// Foreign key to the campaign.
        /// </summary>
        public Guid CampaignId { get; set; }
        /// <summary>
        /// Navigation property pointing to the campaign.
        /// </summary>
        public virtual DbCampaign Campaign { get; set; }
    }
}
