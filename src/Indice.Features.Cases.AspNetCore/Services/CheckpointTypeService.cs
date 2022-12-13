using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class CheckpointTypeService : ICheckpointTypeService
    {
        private readonly CasesDbContext _dbContext;

        public CheckpointTypeService(CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<string>> GetDistinctCheckpointCodes(ClaimsPrincipal user) {
            if (user.IsAdmin()) {
                return await GetAdminDistinctCheckpointNames();
            }

            var roleClaims = user.Claims
               .Where(c => c.Type == JwtClaimTypes.Role)
               .Select(c => c.Value)
               .ToList();

            var checkpointTypeIds = await _dbContext.RoleCaseTypes
                .AsQueryable()
                .Where(r => roleClaims.Contains(r.RoleName))
                .Select(c => c.CheckpointTypeId)
                .ToListAsync();

            var checkpointTypes = await _dbContext.CheckpointTypes
                .AsQueryable()
                .Where(c => checkpointTypeIds.Contains(c.Id))
                .Select(c => c.Code)
                .AsAsyncEnumerable()
                .Distinct() // TODO client-side evaluation, this needs to change
                .ToListAsync();
            return checkpointTypes;
        }

        private async Task<List<string>> GetAdminDistinctCheckpointNames() {
            return await _dbContext.CheckpointTypes
                .AsQueryable()
                .Select(c => c.Code)
                .AsAsyncEnumerable()
                .Distinct() // TODO client-side evaluation, this needs to change
                .ToListAsync();
        }
    }
}