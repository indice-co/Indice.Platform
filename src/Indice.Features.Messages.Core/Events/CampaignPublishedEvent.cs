using System.Dynamic;
using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Events
{
    /// <summary>The event model used when a new campaign is created.</summary>
    public class CampaignPublishedEvent
    {
        private Dictionary<string, MessageContent> _content = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The unique identifier of the campaign.</summary>
        public Guid Id { get; set; }
        /// <summary>The content of the campaign.</summary>
        public Dictionary<string, MessageContent> Content {
            get { return _content; }
            set { _content = new Dictionary<string, MessageContent>(value, StringComparer.OrdinalIgnoreCase); }
        }
        /// <summary>Determines if a campaign is published.</summary>
        public bool Published { get; set; }
        /// <summary>Specifies the time period that a campaign is active.</summary>
        public Period ActivePeriod { get; set; }
        /// <summary>Determines if campaign targets all user base.</summary>
        public bool IsGlobal { get; set; }
        /// <summary>Optional data for the campaign.</summary>
        public ExpandoObject Data { get; set; }
        /// <summary>The delivery channel of a campaign.</summary>
        public MessageChannelKind MessageChannelKind { get; set; }
        /// <summary>The distribution list of the campaign.</summary>
        public Guid? DistributionListId { get; set; }
        /// <summary>Determines whether the distribution list already exists or is new.</summary>
        public bool IsNewDistributionList { get; set; }
        /// <summary>The type details of the campaign.</summary>
        public MessageType Type { get; set; }
        /// <summary>Defines a list of user identifiers that constitutes the audience of the campaign.</summary>
        public List<string> RecipientIds { get; set; } = new List<string>();
        /// <summary>
        /// List of anonymous contacts not available through any of the existing contact resolvers.
        /// Use this list if recipient id is not known/available or the message will be fire and forget.
        /// </summary>
        public List<ContactAnonymous> Recipients { get; set; } = new List<ContactAnonymous>();

        /// <summary>Creates a <see cref="CampaignPublishedEvent"/> instance from a <see cref="Campaign"/> instance.</summary>
        /// <param name="campaign">Models a campaign.</param>
        /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
        /// <param name="recipients">Defines a list of aditional anonymous contacts to be also audience of the campaign.</param>
        /// <param name="isNewDistributionList">Determines whether the distribution list already exists or is new.</param>
        public static CampaignPublishedEvent FromCampaign(Campaign campaign, List<string> recipientIds = null, List<ContactAnonymous> recipients = null, bool isNewDistributionList = true) => new() {
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            Data = campaign.Data,
            MessageChannelKind = campaign.MessageChannelKind,
            DistributionListId = campaign.DistributionList?.Id,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            IsNewDistributionList = isNewDistributionList,
            Published = campaign.Published,
            RecipientIds = recipientIds ?? new List<string>(),
            Recipients = recipients ?? new List<ContactAnonymous>()
        };
    }
}
