using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface ICampaignService
    {
        Task AssociateCampaignImage(Guid campaignId, Guid attachmentId);
        Task<AttachmentLink> CreateAttachment(IFormFile file);
        Task<Campaign> CreateCampaign(CreateCampaignRequest request);
        Task DeleteCampaign(Guid campaignId);
        Task<CampaignDetails> GetCampaignById(Guid campaignId);
        Task<ResultSet<Campaign>> GetCampaigns(ListOptions options);
        Task<CampaignStatistics> GetCampaignStatistics(Guid campaignId);
        Task UpdateCampaign(Guid campaignId, UpdateCampaignRequest request);
    }
}
