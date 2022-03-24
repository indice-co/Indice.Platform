using System;
using System.IO;
using System.Linq.Expressions;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    internal class Mapper
    {
        public static Expression<Func<DbCampaign, Campaign>> ProjectToCampaign = campaign => new () {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            DistributionList = campaign.DistributionList != null ? new DistributionList {
                Id = campaign.DistributionList.Id,
                Name = campaign.DistributionList.Name
            } : null,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published,
            Title = campaign.Title,
            Type = campaign.Type != null ? new MessageType {
                Id = campaign.Type.Id,
                Name = campaign.Type.Name
            } : null
        };

        public static Expression<Func<DbCampaign, CampaignDetails>> ProjectToCampaignDetails = campaign => new () {
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            ActivePeriod = campaign.ActivePeriod,
            Attachment = campaign.Attachment != null ? new AttachmentLink {
                Id = campaign.Attachment.Id,
                FileGuid = campaign.Attachment.Guid,
                ContentType = campaign.Attachment.ContentType,
                Label = campaign.Attachment.Name,
                Size = campaign.Attachment.ContentLength,
                PermaLink = $"/campaigns/attachments/{(Base64Id)campaign.Attachment.Guid}.{Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}"
            } : null,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            DistributionList = campaign.DistributionList != null ? new DistributionList {
                Id = campaign.DistributionList.Id,
                Name = campaign.DistributionList.Name
            } : null,
            Id = campaign.Id,
            Published = campaign.Published,
            IsGlobal = campaign.IsGlobal,
            Title = campaign.Title,
            Type = campaign.Type != null ? new MessageType {
                Id = campaign.Type.Id,
                Name = campaign.Type.Name
            } : null
        };
    }
}
