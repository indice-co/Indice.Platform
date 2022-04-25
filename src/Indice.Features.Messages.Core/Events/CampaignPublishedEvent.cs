using System.Dynamic;
using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Events
{
    /// <summary>
    /// The event model used when a new campaign is created.
    /// </summary>
    public class CampaignPublishedEvent
    {
        private Dictionary<string, MessageContent> _content = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public Dictionary<string, MessageContent> Content {
            get { return _content; }
            set { _content = new Dictionary<string, MessageContent>(value, StringComparer.OrdinalIgnoreCase); }
        }
        /// <summary>
        /// Determines if a campaign is published.
        /// </summary>
        public bool Published { get; set; }
        /// <summary>
        /// Specifies the time period that a campaign is active.
        /// </summary>
        public Period ActivePeriod { get; set; }
        /// <summary>
        /// Determines if campaign targets all user base.
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public ExpandoObject Data { get; set; }
        /// <summary>
        /// The delivery channel of a campaign.
        /// </summary>
        public MessageChannelKind MessageChannelKind { get; set; }
        /// <summary>
        /// The distribution list of the campaign.
        /// </summary>
        public Guid? DistributionListId { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public MessageType Type { get; set; }
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> RecipientIds { get; set; } = new List<string>();

        /// <summary>
        /// Creates a <see cref="CampaignPublishedEvent"/> instance from a <see cref="Campaign"/> instance.
        /// </summary>
        /// <param name="campaign">Models a campaign.</param>
        /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
        public static CampaignPublishedEvent FromCampaign(Campaign campaign, List<string> recipientIds = null) => new() {
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            Data = campaign.Data,
            MessageChannelKind = campaign.MessageChannelKind,
            DistributionListId = campaign.DistributionList?.Id,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published,
            RecipientIds = recipientIds ?? new List<string>()
        };
    }
}
