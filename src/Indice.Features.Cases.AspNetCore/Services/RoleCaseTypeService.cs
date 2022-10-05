using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Cases.Services
{
    internal class RoleCaseTypeService : ICaseAuthorizationService
    {
        private readonly CasesDbContext _dbContext;
        private readonly IDistributedCache _distributedCache;
        private readonly string _roleCaseTypesCacheKey = $"{nameof(RoleCaseTypeService)}.roleCaseTypes";

        public RoleCaseTypeService(
            CasesDbContext dbContext,
            IDistributedCache distributedCache
            ) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public async Task<GetCasesListFilter> ApplyFilterFor(ClaimsPrincipal user, GetCasesListFilter filter) {
            if (filter is null) {
                throw new ArgumentNullException(nameof(filter));
            }
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }

            // if client is systemic, then bypass checks
            if ((user.HasClaim(JwtClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin()) {
                return filter;
            }

            var roleClaims = GetRoleClaims(user);
            var roleCaseTypes = await GetRoleCaseTypes();

            filter.CheckpointTypeCodes = ApplyCheckpointTypeFilter(filter.CheckpointTypeCodes, roleClaims, roleCaseTypes);
            filter.CaseTypeCodes = ApplyCaseTypeFilter(filter.CaseTypeCodes, roleClaims, roleCaseTypes);

            // TODO: admin is not enough, we probably need to get the "get all cases" role from config
            if (!user.IsAdmin()
               && ((filter.CaseTypeCodes == null || filter.CaseTypeCodes.Count() == 0)
               || (filter.CheckpointTypeCodes == null || filter.CheckpointTypeCodes.Count() == 0))) {
                // if the user is not an Admin, and comes with no available caseTypes or CheckpointTypes to see, 
                // tough luck!
                throw new ResourceUnauthorizedException("User has access to no cases");
            }

            return filter;
        }

        public async Task<bool> IsValid(ClaimsPrincipal user, CaseDetails caseDetails) {
            if (caseDetails == null) throw new ArgumentNullException(nameof(caseDetails));
            if (user is null) throw new ArgumentNullException(nameof(user));

            // if client is systemic, then bypass checks
            if ((user.HasClaim(JwtClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin() || IsOwnerOfCase(user, caseDetails)) {
                return true;
            }

            var roleClaims = GetRoleClaims(user);
            var roleCaseTypes = await GetRoleCaseTypes();

            var allowedIdCombinations = roleCaseTypes
                .Where(c => roleClaims.Contains(c.RoleName!))
                .ToList();

            return allowedIdCombinations
                .Any(x => x.CaseTypeId == caseDetails.CaseType!.Id && x.CheckpointTypeId == caseDetails.CheckpointTypeId);
        }

        private bool IsOwnerOfCase(ClaimsPrincipal user, CaseDetails caseDetails) {
            return user.FindSubjectId().Equals(caseDetails.CreatedById);
        }

        private List<string>? ApplyCheckpointTypeFilter(List<string>? checkpointTypeCodes, List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
            var allowedCheckpointTypeCodes = roleCaseTypes
                .Where(c => roleClaims.Contains(c.RoleName!))
                .Select(x => x.CheckpointType.Code)
                .ToList();
            // fuzzy match the CheckPointType Code
            var allowedRelativeCheckpointTypeCodes = checkpointTypeCodes != null
                ? allowedCheckpointTypeCodes.Where(a => checkpointTypeCodes.Any(a.Contains)).ToList()
                : Enumerable.Empty<string>();
            return checkpointTypeCodes is null ? allowedCheckpointTypeCodes : allowedCheckpointTypeCodes.Intersect(allowedRelativeCheckpointTypeCodes).ToList();
        }

        private List<string>? ApplyCaseTypeFilter(List<string>? caseTypeCodes, List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
            var allowedCaseTypeCodes = roleCaseTypes
                .Where(c => roleClaims.Contains(c.RoleName!))
                .Select(x => x.CaseType.Code)
                .Distinct()
                .ToList();

            return caseTypeCodes is null ? allowedCaseTypeCodes : allowedCaseTypeCodes.Intersect(caseTypeCodes).ToList();
        }

        private async Task<List<RoleCaseType>> GetRoleCaseTypes() {
            return await _distributedCache.TryGetAndSetAsync(
                cacheKey: $"{_roleCaseTypesCacheKey}",
                getSourceAsync: async () => await _dbContext.RoleCaseTypes
                   .Include(c => c.CaseType)
                   .Include(c => c.CheckpointType)
                   .Select(c => new RoleCaseType {
                       Id = c.Id,
                       RoleName = c.RoleName,
                       CaseTypeId = c.CaseTypeId,
                       CheckpointTypeId = c.CheckpointTypeId,
                       CaseType = new CaseTypePartial {
                           Id = c.CaseTypeId,
                           Code = c.CaseType!.Code
                       },
                       CheckpointType = new CheckpointType {
                           Id = c.CheckpointTypeId,
                           Code = c.CheckpointType!.Code
                       }
                   })
                   .ToListAsync(),
                options: new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
        }

        private List<string> GetRoleClaims(ClaimsPrincipal user) {
            return user.Claims
                    .Where(c => c.Type == JwtClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();
        }
    }
}