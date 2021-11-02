using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface ICampaignService
    {
        Task<ResultSet<Campaign>> GetCampaigns(ListOptions options);
        Task<CampaignDetails> GetCampaignById(Guid campaignId);
    }
}