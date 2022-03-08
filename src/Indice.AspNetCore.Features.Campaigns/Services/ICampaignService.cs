using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface ICampaignService
    {
        Task<ResultSet<Campaign>> GetCampaigns(ListOptions<GetCampaignsListFilter> options);
        Task<CampaignDetails> GetCampaignById(Guid campaignId);
        Task<Campaign> CreateCampaign(CreateCampaignRequest request);
        Task UpdateCampaign(Guid campaignId, UpdateCampaignRequest request);
        Task DeleteCampaign(Guid campaignId);
        Task<AttachmentLink> CreateAttachment(IFormFile file);
        Task AssociateCampaignAttachment(Guid campaignId, Guid attachmentId);
        Task<ResultSet<MessageType>> GetMessageTypes(ListOptions options);
        Task<MessageType> GetMessageTypeById(Guid campaignTypeId);
        Task<MessageType> GetMessageTypeByName(string name);
        Task<MessageType> CreateMessageType(UpsertMessageTypeRequest request);
        Task UpdateMessageType(Guid campaignTypeId, UpsertMessageTypeRequest request);
        Task DeleteMessageType(Guid campaignTypeId);
        Task<CampaignStatistics> GetCampaignStatistics(Guid campaignId);
        Task UpdateCampaignHit(Guid campaignId);
    }
}
