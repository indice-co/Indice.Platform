using System.Dynamic;
using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Data.Models
{
    /// <summary>
    /// Campaign entity.
    /// </summary>
    public class DbCampaign
    {
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Defines a (call-to-action) link.
        /// </summary>
        public Hyperlink ActionLink { get; set; }
        /// <summary>
        /// Specifies when a campaign was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
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
        /// Foreign key to the <see cref="DbMessageType"/>.
        /// </summary>
        public Guid? TypeId { get; set; }
        /// <summary>
        /// Foreign key to the <see cref="DbAttachment"/>.
        /// </summary>
        public Guid? AttachmentId { get; set; }
        /// <summary>
        /// Foreign key to the <see cref="DbDistributionList"/>.
        /// </summary>
        public Guid? DistributionListId { get; set; }
        /// <summary>
        /// Foreign key to the <see cref="DbTemplate"/>.
        /// </summary>
        public Guid TemplateId { get; set; }
        /// <summary>
        /// An attachment object for the campaign.
        /// </summary>
        public virtual DbAttachment Attachment { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public virtual DbMessageType Type { get; set; }
        /// <summary>
        /// The distribution list of the campaign.
        /// </summary>
        public virtual DbDistributionList DistributionList { get; set; }
        /// <summary>
        /// The template of the campaign.
        /// </summary>
        public virtual DbTemplate Template { get; set; }
    }
}
