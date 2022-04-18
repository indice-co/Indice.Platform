using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>
    /// The request model used to update an existing campaign.
    /// </summary>
    public class UpdateCampaignRequest
    {
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Defines a (call-to-action) link.
        /// </summary>
        public Hyperlink ActionLink { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// The id of the type this campaign belongs.
        /// </summary>
        public Guid? TypeId { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public dynamic Data { get; set; }
    }
}
