using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class DistributionListService : IDistributionListService
    {
        public DistributionListService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public CampaignsDbContext DbContext { get; }

        public async Task<DistributionList> CreateDistributionList(CreateDistributionListRequest distributionList) {
            var newList = new DbDistributionList {
                Id = Guid.NewGuid(),
                Name = distributionList.Name
            };
            DbContext.DistributionLists.Add(newList);
            await DbContext.SaveChangesAsync();
            return new DistributionList {
                Id = newList.Id,
                Name = newList.Name
            };
        }

        public async Task<DistributionList> GetDistributionListByName(string name) {
            var list = await DbContext.DistributionLists.SingleOrDefaultAsync(x => x.Name == name);
            if (list is null) {
                return default;
            }
            return new DistributionList {
                Id = list.Id,
                Name = list.Name
            };
        }

        public Task<ResultSet<DistributionList>> GetDistributionLists(ListOptions options) =>
            DbContext.DistributionLists
                     .AsNoTracking()
                     .Select(campaignType => new DistributionList {
                         Id = campaignType.Id,
                         Name = campaignType.Name
                     })
                     .ToResultSetAsync(options);
    }
}
