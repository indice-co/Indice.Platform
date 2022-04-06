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
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>();
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
        public MessageChannelKind DeliveryChannel { get; set; }
        /// <summary>
        /// The distribution list of the campaign.
        /// </summary>
        public Guid? DistributionListId { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Creates a <see cref="CampaignPublishedEvent"/> instance from a <see cref="Campaign"/> instance.
        /// </summary>
        /// <param name="campaign">Models a campaign.</param>
        public static CampaignPublishedEvent FromCampaign(Campaign campaign) => new() {
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            DistributionListId = campaign.DistributionList?.Id,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published
        };
    }
}
