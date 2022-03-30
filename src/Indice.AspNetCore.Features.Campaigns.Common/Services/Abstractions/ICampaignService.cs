using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public interface ICampaignService
    {
        Task<ResultSet<Campaign>> GetList(ListOptions<CampaignsFilter> options);
        Task<CampaignDetails> GetById(Guid id);
        Task<Campaign> Create(CreateCampaignRequest request);
        Task Update(Guid id, UpdateCampaignRequest request);
        Task Delete(Guid id);
        Task Publish(Guid id);
        Task<AttachmentLink> CreateAttachment(FileAttachment fileAttachment);
        Task AssociateAttachment(Guid id, Guid attachmentId);
        Task<CampaignStatistics> GetStatistics(Guid id);
        Task UpdateHit(Guid id);
    }
}
