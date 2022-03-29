using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface ICampaignService
    {
        Task<ResultSet<Campaign>> GetList(ListOptions<CampaignsFilter> options);
        Task<CampaignDetails> GetById(Guid campaignId);
        Task<Campaign> Create(CreateCampaignRequest request);
        Task<bool> Update(Guid campaignId, UpdateCampaignRequest request);
        Task<bool> Delete(Guid campaignId);
        Task<bool> Publish(Guid campaignId);
        Task<AttachmentLink> CreateAttachment(IFormFile file);
        Task AssociateAttachment(Guid campaignId, Guid attachmentId);
        Task<CampaignStatistics> GetStatistics(Guid campaignId);
        Task UpdateHit(Guid campaignId);
    }
}
