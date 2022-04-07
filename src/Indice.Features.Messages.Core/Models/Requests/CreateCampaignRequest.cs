using System.Dynamic;
using System.Text.Json.Serialization;
using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>
    /// The request model used to create a new campaign.
    /// </summary>
    public class CreateCampaignRequest
    {
        /// <summary>
        /// The id of the campaign.
        /// </summary>
        [JsonIgnore]
        public Guid Id { get; set; }
        /// <summary>
        /// Determines if campaign targets all user base. Defaults to true.
        /// </summary>
        public bool IsGlobal { get; set; } = true;
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> RecipientIds { get; set; } = new List<string>();
        /// <summary>
        /// The delivery channel of a campaign. Default is <see cref="MessageChannelKind.Inbox"/>.
        /// </summary>
        public MessageChannelKind MessageChannelKind { get; set; } = MessageChannelKind.Inbox;
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The contents of the campaign.
        /// </summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Defines a (call-to-action) link.
        /// </summary>
        public Hyperlink ActionLink { get; set; }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool Published { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// The id of the type this campaign belongs.
        /// </summary>
        public Guid? TypeId { get; set; }
        /// <summary>
        /// The id of the distribution list.
        /// </summary>
        public Guid? DistributionListId { get; set; }
        /// <summary>
        /// The id of the template.
        /// </summary>
        public Guid? TemplateId { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public ExpandoObject Data { get; set; }
    }
}
