using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Cases.Services;

internal class MemberAuthorizationService : ICaseAuthorizationService
{
    private readonly CasesDbContext _dbContext;
    private readonly IDistributedCache _distributedCache;
    private const string MembersCacheKey = $"{nameof(MemberAuthorizationService)}.members";

    public MemberAuthorizationService(
        CasesDbContext dbContext,
        IDistributedCache distributedCache
        ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
    }
    /// <summary>
    /// In the case of a non admin user. Apply extra Where clauses to the IQueryable based on their roles.
    /// </summary>
    /// <param name="cases"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> cases, ClaimsPrincipal user) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        if ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin()) {
            return cases;
        }

        // if the user is not systemic or admin then based on their role they have access to specific checkpoint types and case type codes.
        var roleClaims = GetUserRoles(user);
        var members = await GetMembers();

        var allowedCheckpointTypeIds = GetAllowedCheckpointTypes(roleClaims, members);
        var allowedCaseTypeCodes = GetAllowedCaseTypeCodes(roleClaims, members);

        if (allowedCheckpointTypeIds is not null && allowedCheckpointTypeIds.Any()) {
            cases = cases.Where(cp => allowedCheckpointTypeIds.Contains(cp.CheckpointTypeId.ToString()));
        }
        if (allowedCaseTypeCodes is not null && allowedCaseTypeCodes.Any()) {
            cases = cases.Where(cp => allowedCaseTypeCodes.Contains(cp.CaseType.Code));
        }
        return cases;
    }

    /// <summary>Determines whether user can see a Case in relation to i) user's role(s) and ii) case's CaseType and CheckpointType</summary>
    /// <param name="user"></param>
    /// <param name="case"></param>
    public async Task<bool> IsValid(ClaimsPrincipal user, Case @case) {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (@case is null) throw new ArgumentNullException(nameof(@case));

        // if client is systemic, then bypass checks
        if ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin() || IsOwnerOfCase(user, @case)) {
            return true;
        }

        var roles = GetUserRoles(user);
        var members = await GetMembers();

        var memberships = members
            .Where(c => roles.Contains(c.RoleName!))
            .ToList();

        return memberships
            .Any(x => x.CaseTypeId == @case.CaseType!.Id && x.CheckpointTypeId == @case.CheckpointTypeId);
    }

    /// <summary>Determines whether user is Owner of a Case</summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    private bool IsOwnerOfCase(ClaimsPrincipal user, Case @case) =>
        user.FindSubjectId().Equals(@case.CreatedById);

    /// <summary>Gets the list of allowed and filtered CheckpointType Ids for admin users</summary>
    /// <param name="checkpointTypeCodesFilterClause"></param>
    private async Task<List<FilterClause>> ApplyAdminCheckpointTypeFilter(List<FilterClause> checkpointTypeCodesFilterClause) {
        // get only the values of the codes
        var checkpointTypeCodes = checkpointTypeCodesFilterClause.Select(f => f.Value).ToList();
        var filteredCheckpointTypesList = new List<DbCheckpointType>();
        // admin didn't select checkpointTypeCode filters -> get all checkpointTypeIds
        if (checkpointTypeCodes is null || checkpointTypeCodes.Count == 0) {
            filteredCheckpointTypesList = await _dbContext.CheckpointTypes
            .AsQueryable()
            .ToListAsync();
        } else {
            // admin selected checkpointTypeCode filters
            filteredCheckpointTypesList = await _dbContext.CheckpointTypes
            .AsQueryable()
            .Where(checkpointType => checkpointTypeCodes.Contains(checkpointType.Code))
            .ToListAsync();
        }
        // create a new list of checkpoint type ids of type FilterClause to include the operator.
        var filteredCheckpointTypeFilterClauses = new List<FilterClause>();
        if (filteredCheckpointTypesList is not null || filteredCheckpointTypesList.Any()) {
            foreach (var checkpointType in filteredCheckpointTypesList) {
                // find the checkpoint type that matches the checkpoint type code given in the parameters
                if (checkpointTypeCodesFilterClause.Select(f => f.Value).Contains(checkpointType.Code)) {
                    var checkpointTypeOperator = checkpointTypeCodesFilterClause.FirstOrDefault(f => f.Value == checkpointType.Code).Operator;
                    // create a new FilterClause object that holds the Id but also the operator
                    var newCheckpointTypeIdFilterClause = new FilterClause("checkpointTypeId", checkpointType.Id.ToString(), checkpointTypeOperator, JsonDataType.String);
                    filteredCheckpointTypeFilterClauses.Add(newCheckpointTypeIdFilterClause);
                }
            }
        }
        return filteredCheckpointTypeFilterClauses;
    }

    /// <summary>Gets the list of allowed and filtered CheckpointType Ids for non-admin users</summary>
    /// <param name="roleClaims"></param>
    /// <param name="roleCaseTypes"></param>
    private List<FilterClause> ApplyCheckpointTypeFilter(List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
        // what CheckpointTypes can the user see based on their ROLE(S)?
        var roleBasedAllowedCheckpointTypes = roleCaseTypes
            .Where(roleCaseType => roleClaims.Contains(roleCaseType.RoleName!))
            .Select(roleCaseType => new { CheckpointTypeId = roleCaseType.CheckpointTypeId.ToString(), CheckpointTypeCode = roleCaseType.CheckpointType.Code })
            .Distinct() // Avoid duplicates: it is possible that user has >1 roles and those roles can "see" common CheckpointTypes
            .ToList();

        var filteredCheckpointTypeFilterClauses = new List<FilterClause>();
        foreach (var checkpointType in roleBasedAllowedCheckpointTypes) {
            // create a new FilterClause object that holds the Id of the checkpoint type.
            filteredCheckpointTypeFilterClauses.Add(new FilterClause("checkpointTypeId", checkpointType.CheckpointTypeId, FilterOperator.Eq, JsonDataType.String));
        }
        return filteredCheckpointTypeFilterClauses;

    }

    /// <summary>Gets the list of allowed and filtered CaseType Codes for non-admin users</summary>
    /// <param name="roleClaims"></param>
    /// <param name="roleCaseTypes"></param>
    private List<FilterClause> ApplyCaseTypeFilter(List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
        // A list of allowed case type codes based on the users role
        var roleBasedAllowedCaseTypeCodes = roleCaseTypes
            .Where(roleCaseType => roleClaims.Contains(roleCaseType.RoleName!))
            .Select(x => x.CaseTypePartial.Code)
            .Distinct()
            .ToList();

        var caseTypeCodeFilterClauses = new List<FilterClause>();
        // map the case type code to a new FilterClause
        foreach (var allowedCaseTypeCode in roleBasedAllowedCaseTypeCodes) {
            caseTypeCodeFilterClauses.Add(new FilterClause("caseTypeCode", allowedCaseTypeCode, FilterOperator.Eq, JsonDataType.String));
        }
        return caseTypeCodeFilterClauses;
    }

    /// <summary>Gets the list of RoleCaseTypes</summary>
    private async Task<List<RoleCaseType>> GetMembers() {
        return await _distributedCache.TryGetAndSetAsync(
            cacheKey: $"{MembersCacheKey}",
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
    private List<string> GetUserRoles(ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type == BasicClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

    private List<string> GetAllowedCheckpointTypes(List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
        // what CheckpointTypes can the user see based on their ROLE(S)?
        return roleCaseTypes
            .Where(roleCaseType => roleClaims.Contains(roleCaseType.RoleName!))
            .Select(roleCaseType => roleCaseType.CheckpointTypeId.ToString())
            .Distinct() // Avoid duplicates: it is possible that user has >1 roles and those roles can "see" common CheckpointTypes
            .ToList();
    }
    private List<string> GetAllowedCaseTypeCodes(List<string> roleClaims, List<RoleCaseType> roleCaseTypes) {
        // what CaseType codes can the user see based on their ROLE(S)?
        return roleCaseTypes
            .Where(roleCaseType => roleClaims.Contains(roleCaseType.RoleName!))
            .Select(x => x.CaseTypePartial.Code)
            .Distinct()
            .ToList();
    }
}