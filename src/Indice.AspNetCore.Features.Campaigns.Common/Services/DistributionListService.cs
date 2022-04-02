using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// An implementation of <see cref="IDistributionListService"/> for Entity Framework Core.
    /// </summary>
    public class DistributionListService : IDistributionListService
    {
        /// <summary>
        /// Creates a new instance of <see cref="DistributionListService"/>.
        /// </summary>
        /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DistributionListService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        private CampaignsDbContext DbContext { get; }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task<ResultSet<Contact>> GetContactsList(Guid id, ListOptions options) {
            var query = DbContext
                .Contacts
                .AsNoTracking()
                .Where(x => x.DistributionListId == id)
                .Select(Mapper.ProjectToContact);
            return await query.ToResultSetAsync(options);
        }

        /// <inheritdoc />
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
