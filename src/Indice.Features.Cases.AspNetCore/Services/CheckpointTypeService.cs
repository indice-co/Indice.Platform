using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class CheckpointTypeService : ICheckpointTypeService
    {
        private readonly CasesDbContext _dbContext;

        public CheckpointTypeService(CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<string>> GetDistinctCheckpointNames(ClaimsPrincipal user) {

            var roleClaims = user.Claims
               .Where(c => c.Type == JwtClaimTypes.Role)
               .Select(c => c.Value)
               .ToList();

            var caseTypeIds = await _dbContext.RoleCaseTypes
                .AsQueryable()
                .Where(r => roleClaims.Contains(r.RoleName))
                .Select(c => c.CaseTypeId)
                .ToListAsync();

            var checkpointTypes = await _dbContext.CheckpointTypes
                .AsQueryable()
                .Where(c => caseTypeIds.Contains(c.CaseTypeId))
                .Select(c =>   c.Name)
                .AsAsyncEnumerable()
                .Distinct()
                .ToListAsync();
            return checkpointTypes;
        }
    }
}