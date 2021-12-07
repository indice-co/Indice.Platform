using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface ICampaignService
    {
        Task AssociateCampaignAttachment(Guid campaignId, Guid attachmentId);
        Task<AttachmentLink> CreateAttachment(IFormFile file);
        Task<Campaign> CreateCampaign(CreateCampaignRequest request);
        Task<CampaignType> CreateCampaignType(CreateCampaignTypeRequest request);
        Task DeleteCampaign(Guid campaignId);
        Task DeleteCampaignType(Guid campaignTypeId);
        Task<CampaignDetails> GetCampaignById(Guid campaignId);
        Task<ResultSet<Campaign>> GetCampaigns(ListOptions<GetCampaignsListFilter> options);
        Task<CampaignStatistics> GetCampaignStatistics(Guid campaignId);
        Task<CampaignType> GetCampaignTypeById(Guid campaignTypeId);
        Task<ResultSet<CampaignType>> GetCampaignTypes(ListOptions options);
        Task UpdateCampaign(Guid campaignId, UpdateCampaignRequest request);
        Task UpdateCampaignVisit(Guid campaignId);
    }
}
