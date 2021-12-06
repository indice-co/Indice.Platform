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
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),
            Published = request.IsActive,
            IsGlobal = request.IsGlobal,
            Title = request.Title,
            TypeId = request.TypeId
        };

        public static Campaign ToCampaign(this DbCampaign campaign) => new() {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Id = campaign.Id,
            Published = campaign.Published,
            IsGlobal = campaign.IsGlobal,
            Title = campaign.Title
        };
    }
}
