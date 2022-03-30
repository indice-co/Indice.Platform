using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public class DistributionListService : IDistributionListService
    {
        public DistributionListService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public CampaignsDbContext DbContext { get; }

        public async Task<DistributionList> Create(CreateDistributionListRequest request) {
            var list = new DbDistributionList {
                Id = Guid.NewGuid(),
                Name = request.Name
            };
            DbContext.DistributionLists.Add(list);
            await DbContext.SaveChangesAsync();
            return new DistributionList {
                Id = list.Id,
                Name = list.Name
            };
        }

        public async Task<DistributionList> GetById(Guid id) {
            var list = await DbContext.DistributionLists.FindAsync(id);
            if (list is null) {
                return default;
            }
            return new DistributionList {
                Id = list.Id,
                Name = list.Name
            };
        }

        public async Task<DistributionList> GetByName(string name) {
            var list = await DbContext.DistributionLists.SingleOrDefaultAsync(x => x.Name == name);
            if (list is null) {
                return default;
            }
            return new DistributionList {
                Id = list.Id,
                Name = list.Name
            };
        }

        public async Task<ResultSet<Contact>> GetContactsList(Guid id, ListOptions options) {
            var query = DbContext
                .Contacts
                .AsNoTracking()
                .Where(x => x.DistributionListId == id)
                .Select(Mapper.ProjectToContact);
            return await query.ToResultSetAsync(options);
        }

        public Task<ResultSet<DistributionList>> GetList(ListOptions options) {
            var query = DbContext
                .DistributionLists
                .AsNoTracking()
                .Select(campaignType => new DistributionList {
                    Id = campaignType.Id,
                    Name = campaignType.Name
                });
            return query.ToResultSetAsync(options);
        }
    }
}
