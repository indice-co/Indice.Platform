using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IDistributionListService
    {
        Task<DistributionList> CreateDistributionList(CreateDistributionListRequest distributionList);
        Task<DistributionList> GetDistributionListByName(string name);
        Task<ResultSet<DistributionList>> GetDistributionLists(ListOptions options);
    }
}
