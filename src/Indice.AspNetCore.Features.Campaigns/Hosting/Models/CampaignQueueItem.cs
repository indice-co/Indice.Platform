using System.Collections.Generic;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class CampaignQueueItem : Campaign
    {
        public List<string> SelectedUserCodes { get; set; } = new List<string>();
    }

    internal static class CampaignExtensions
    {
        public static CampaignQueueItem ToCampaignQueueItem(this Campaign campaign, List<string> selectedUserCodes = null) => new() {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published,
            SelectedUserCodes = selectedUserCodes ?? new List<string>(),
            Title = campaign.Title,
            Type = campaign.Type
        };
    }
}
