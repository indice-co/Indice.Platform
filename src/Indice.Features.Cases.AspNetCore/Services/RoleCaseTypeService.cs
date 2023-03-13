using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Cases.Services;

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

    /// <summary>Applies Filters in relation to user's role(s)</summary>
    /// <param name="user"></param>
    /// <param name="filter"></param>
    public async Task<GetCasesListFilter> ApplyFilterFor(ClaimsPrincipal user, GetCasesListFilter filter) {
        if (filter is null) {
            throw new ArgumentNullException(nameof(filter));
        }
        if (user is null) {
            throw new ArgumentNullException(nameof(user));
        }

        // if client is systemic or admin, then bypass checks
        if ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin()) {
            filter.CheckpointTypeCodes = await ApplyAdminCheckpointTypeFilter(filter.CheckpointTypeCodes);
            // no need to do anything for CaseType codes if user is systemic or admin!
            return filter;
        }

        var roleClaims = GetRoleClaims(user);
        var roleCaseTypes = await GetRoleCaseTypes();

        filter.CheckpointTypeIds = ApplyCheckpointTypeFilter(filter.CheckpointTypeCodes, roleClaims, roleCaseTypes);
        filter.CaseTypeCodes = ApplyCaseTypeFilter(filter.CaseTypeCodes, roleClaims, roleCaseTypes);
        
        if ((filter.CaseTypeCodes is null || filter.CaseTypeCodes.Count == 0) ||
            (filter.CheckpointTypeIds is null || filter.CheckpointTypeIds.Count == 0)) {
            // if the (non-admin) user comes with no available caseTypes or CheckpointTypes to see, tough luck!
            throw new ResourceUnauthorizedException("User has access to no cases");
        }

        return filter;
    }

    /// <summary>Determines whether user can see a Case in relation to i) user's role(s) and ii) case's CaseType and CheckpointType</summary>
    /// <param name="user"></param>
    /// <param name="case"></param>
    public async Task<bool> IsValid(ClaimsPrincipal user, Case @case) {
        if (@case == null) throw new ArgumentNullException(nameof(@case));
        if (user is null) throw new ArgumentNullException(nameof(user));

        // if client is systemic, then bypass checks
        if ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin() || IsOwnerOfCase(user, @case)) {
            return true;
        }

        var roleClaims = GetRoleClaims(user);
        var roleCaseTypes = await GetRoleCaseTypes();

        var allowedIdCombinations = roleCaseTypes
            .Where(c => roleClaims.Contains(c.RoleName!))
            .ToList();

        return allowedIdCombinations
            .Any(x => x.CaseTypeId == @case.CaseType!.Id && x.CheckpointTypeId == @case.CheckpointTypeId);
    }

    /// <summary>Determines whether user is Owner of a Case</summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    private bool IsOwnerOfCase(ClaimsPrincipal user, Case @case) {
        return user.FindSubjectId().Equals(@case.CreatedById);
    }

    /// <summary>Gets the list of allowed and filtered CheckpointType Ids for admin users</summary>
    /// <param name="checkpointTypeCodes"></param>
    private async Task<List<string>> ApplyAdminCheckpointTypeFilter(List<string> checkpointTypeCodes) {
        // admin didn't select checkpointTypeCode filters -> get all checkpointTypeIds
        if (checkpointTypeCodes is null || checkpointTypeCodes.Count == 0) {
            return await _dbContext.CheckpointTypes
            .AsQueryable()
            .Select(checkpointType => checkpointType.Id.ToString())
            .ToListAsync();
        }
        // admin selected checkpointTypeCode filters
        return await _dbContext.CheckpointTypes
            .AsQueryable()
            .Where(checkpointType => checkpointTypeCodes.Contains(checkpointType.Code))
            .Select(checkpointType => checkpointType.Id.ToString())
            .ToListAsync();
    }

    /// <summary>Gets the list of allowed and filtered CheckpointType Ids for non-admin users</summary>
    /// <param name="checkpointTypeCodes"></param>
    /// <param name="roleClaims"></param>
    /// <param name="roleCaseTypes"></param>
    private List<string> ApplyCheckpointTypeFilter(List<string> checkpointTypeCodes, List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
        // what CheckpointTypes can the user see based on his/her ROLE(S)?
        var roleBasedAllowedCheckpointTypes = roleCaseTypes
            .Where(roleCaseType => roleClaims.Contains(roleCaseType.RoleName!))
            .Select(roleCaseType => new { CheckpointTypeId = roleCaseType.CheckpointTypeId.ToString(), CheckpointTypeCode = roleCaseType.CheckpointType.Code })
            .Distinct() // Avoid duplicates: it is possible that user has >1 roles and those roles can "see" common CheckpointTypes
            .ToList();

        return
            checkpointTypeCodes is not null // did user selected a checkpointTypeCode filter?
            ? roleBasedAllowedCheckpointTypes
                .Where(x => checkpointTypeCodes.Contains(x.CheckpointTypeCode))
                .Select(x => x.CheckpointTypeId)
                .ToList()
            : roleBasedAllowedCheckpointTypes
                .Select(x => x.CheckpointTypeId)
                .ToList();
    }

    /// <summary>Gets the list of allowed and filtered CaseType Codes for non-admin users</summary>
    /// <param name="caseTypeCodes"></param>
    /// <param name="roleClaims"></param>
    /// <param name="roleCaseTypes"></param>
    private List<string> ApplyCaseTypeFilter(List<string> caseTypeCodes, List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
        var allowedCaseTypeCodes = roleCaseTypes
            .Where(roleCaseType => roleClaims.Contains(roleCaseType.RoleName!))
            .Select(x => x.CaseTypePartial.Code)
            .Distinct()
            .ToList();

        return caseTypeCodes is null ? allowedCaseTypeCodes : allowedCaseTypeCodes.Intersect(caseTypeCodes).ToList();
    }

    /// <summary>Gets the list of RoleCaseTypes</summary>
    private async Task<List<RoleCaseType>> GetRoleCaseTypes() {
        return await _distributedCache.TryGetAndSetAsync(
            cacheKey: $"{_roleCaseTypesCacheKey}",
            getSourceAsync: async () => await _dbContext.Members
                .AsQueryable()
                .Select(c => new RoleCaseType {
                   Id = c.Id,
                   RoleName = c.RoleName,
                   CaseTypeId = c.CaseTypeId,
                   CheckpointTypeId = c.CheckpointTypeId,
                   CaseTypePartial = new CaseTypePartial {
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

    /// <summary>Gets user's list of Role Claims</summary>
    /// <param name="user"></param>
    private List<string> GetRoleClaims(ClaimsPrincipal user) {
        return user.Claims
                .Where(c => c.Type == BasicClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
    }
}