using System;
using System.Collections.Generic;
using System.Dynamic;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Events;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Models a campaign.
    /// </summary>
    public class Campaign
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
        public string Content { get; set; }
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
        /// The type details of the campaign.
        /// </summary>
        public CampaignType Type { get; set; }
        /// <summary>
        /// The delivery channel of a campaign.
        /// </summary>
        public CampaignDeliveryChannel DeliveryChannel { get; set; }
        /// <summary>
        /// Optional data for the campaign.
        /// </summary>
        public ExpandoObject Data { get; set; }
    }

    /// <summary>
    /// Extension methods on <see cref="Campaign"/> model.
    /// </summary>
    public static class CampaignExtensions
    {
        /// <summary>
        /// Converts a <see cref="Campaign"/> model to it's corresponding <see cref="CampaignQueueItem"/> type.
        /// </summary>
        /// <param name="campaign">The campaign to convert.</param>
        /// <param name="selectedUserCodes">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
        public static CampaignQueueItem ToCampaignQueueItem(this Campaign campaign, List<string> selectedUserCodes = null) => new() {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = (CampaignQueueItem.CampaignDeliveryChannel)(int)campaign.DeliveryChannel,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published,
            SelectedUserCodes = selectedUserCodes ?? new List<string>(),
            Title = campaign.Title,
            Type = campaign.Type != default ? new CampaignQueueItem.CampaignType { 
                Id = campaign.Type.Id,
                Name = campaign.Type.Name
            } : default
        };
    }
}
