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
            Data = request.Data,
            DeliveryType = request.DeliveryType,
            Id = Guid.NewGuid(),
            IsGlobal = request.IsGlobal,
            Published = request.IsActive,
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
            Title = campaign.Title,
            Type = campaign.Type != null ? new CampaignType {
                Id = campaign.Type.Id,
                Name = campaign.Type.Name
            } : null
        };
    }
}
