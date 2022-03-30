using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IDistributionListService
    {
        Task<DistributionList> Create(CreateDistributionListRequest request);
        Task<DistributionList> GetById(Guid id);
        Task<DistributionList> GetByName(string name);
        Task<ResultSet<DistributionList>> GetList(ListOptions options);
        Task<ResultSet<Contact>> GetContactsList(Guid id, ListOptions options);
    }
}
