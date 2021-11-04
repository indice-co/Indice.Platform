using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    internal static class ModelMappingExtensions
    {
        public static DbCampaign ToDbCampaign(this CreateCampaignRequest request) => new() {
            ActionText = request.ActionText,
            ActionUrl = request.ActionUrl,
            ActivePeriod = request.ActivePeriod,
            AttachmentId = request.AttachmentId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),
            IsActive = request.IsActive,
            IsGlobal = request.IsGlobal,
            IsNotification = request.IsNotification,
            Title = request.Title
        };

        public static Campaign ToCampaign(this DbCampaign campaign) => new() {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Id = campaign.Id,
            IsActive = campaign.IsActive,
            IsGlobal = campaign.IsGlobal,
            IsNotification = campaign.IsNotification,
            Title = campaign.Title
        };
    }
}
