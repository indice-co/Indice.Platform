using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Manager.Commands
{
    /// <summary>The command model used to create a new campaign using <see cref="NotificationsManager"/>.</summary>
    public class CreateCampaignCommand
    {
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; internal set; }
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public Dictionary<MessageChannelKind, MessageContent> Content { get; set; } = new Dictionary<MessageChannelKind, MessageContent>();
        /// <summary>
        /// Defines a (call-to-action) link.
        /// </summary>
        public Hyperlink ActionLink { get; set; }
        /// <summary>
        /// Specifies when a campaign was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; internal set; }
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
        /// The type details of the campaign.
        /// </summary>
        public MessageType Type { get; set; }
        /// <summary>
        /// The distribution list of the campaign. Alternatively you can set the <see cref="RecipientIds"/> property.
        /// </summary>
        public Guid? DistributionListId { get; set; }
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> RecipientIds { get; set; } = new List<string>();
        /// <summary>
        /// List of anonymous contacts not available through any of the existing contact resolvers.
        /// Use this list if recipient id is not known/available or the message will be fire and forget.
        /// </summary>
        public List<ContactAnonymous> Recipients { get; set; } = new List<ContactAnonymous>();
        /// <summary>
        /// The delivery channel of a campaign.
        /// </summary>
        public MessageChannelKind MessageChannelKind { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public dynamic Data { get; set; }
    }
}
