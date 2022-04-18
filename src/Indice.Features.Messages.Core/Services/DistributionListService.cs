using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services
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
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = request.CreatedBy,
                Id = Guid.NewGuid(),
                Name = request.Name
            };
            DbContext.DistributionLists.Add(list);
            await DbContext.SaveChangesAsync();
            return new DistributionList {
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
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
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
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
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
                Id = list.Id,
                Name = list.Name
            };
        }

        /// <inheritdoc />
        public Task<ResultSet<DistributionList>> GetList(ListOptions options) {
            var query = DbContext.DistributionLists.AsNoTracking().Select(list => new DistributionList {
                CreatedAt = list.CreatedAt,
                CreatedBy = list.CreatedBy,
                Id = list.Id,
                Name = list.Name
            });
            return query.ToResultSetAsync(options);
        }
    }
}
