using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when a new campaign is created.
    /// </summary>
    public class CampaignCreatedEvent
    {
        /// <summary>
        /// The unique identifier of the campaign.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The title of the campaign.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The content of the campaign.
        /// </summary>
        public CampaignContent Content { get; set; }
        /// <summary>
        /// Defines a CTA (call-to-action) text.
        /// </summary>
        public string ActionText { get; set; }
        /// <summary>
        /// Defines a CTA (call-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
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
        public MessageDeliveryChannel DeliveryChannel { get; set; }
        /// <summary>
        /// The type details of the campaign.
        /// </summary>
        public MessageType Type { get; set; }
        /// <summary>
        /// The distribution list of the campaign.
        /// </summary>
        public DistributionList DistributionList { get; set; }
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> SelectedUserCodes { get; set; } = new List<string>();

        /// <summary>
        /// Creates a <see cref="CampaignCreatedEvent"/> instance from a <see cref="Campaign"/> instance.
        /// </summary>
        /// <param name="campaign">Models a campaign.</param>
        /// <param name="selectedUserCodes">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
        public static CampaignCreatedEvent FromCampaign(Campaign campaign, List<string> selectedUserCodes = null) => new() {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            DistributionList = campaign.DistributionList,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published,
            SelectedUserCodes = selectedUserCodes ?? new List<string>(),
            Title = campaign.Title,
            Type = campaign.Type
        };
    }
}
