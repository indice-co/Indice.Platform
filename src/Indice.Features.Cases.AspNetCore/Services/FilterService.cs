using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class FilterService : IFilterService
    {
        private readonly CasesDbContext _dbContext;

        public FilterService(CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<Filter>> GetFilters(ClaimsPrincipal user) {
            var filters = await _dbContext.Filters
                .AsQueryable()
                .Where(f => f.UserId == user.FindSubjectId())
                .Select(f => new Filter {
                    Id = f.Id,
                    Name = f.Name,
                    QueryParameters = f.QueryParameters
                })
                .ToListAsync();
            return filters;
        }

        public async Task SaveFilter(ClaimsPrincipal user, SaveFilterRequest request) {
            var filter = new DbFilter {
                Id = Guid.NewGuid(),
                UserId = user.FindSubjectId(),
                Name = request.Name,
                QueryParameters = request.QueryParameters,
            };
            // Create filter
            await _dbContext.AddAsync(filter);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteFilter(ClaimsPrincipal user, Guid filterId) {
            var dbFilter = await Get(filterId);
            // someone tries to delete someone else's filter!
            if (dbFilter.UserId != user.FindSubjectId()) {
                throw new Exception("Filter is invalid.");
            }
            _dbContext.Filters.Remove(dbFilter);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<DbFilter> Get(Guid filterId) {
            if (filterId == Guid.Empty) {
                throw new ArgumentNullException(nameof(filterId));
            }
            var dbFilter = await _dbContext.Filters.FindAsync(filterId);
            return dbFilter ?? throw new Exception("Filter is invalid.");
        }
    }
}