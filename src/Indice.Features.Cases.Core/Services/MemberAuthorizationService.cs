using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

internal class MemberAuthorizationService : ICaseAuthorizationService
{
    private readonly CasesDbContext _dbContext;
    private readonly IDistributedCache _distributedCache;
    private readonly CasesOptions _options;
    private const string MembersCacheKey = $"{nameof(MemberAuthorizationService)}.members";

    /// <summary>
    /// A service that determines which cases can the user access based on their role.
    /// </summary>
    public MemberAuthorizationService(
        CasesDbContext dbContext,
        IDistributedCache distributedCache,
        IOptions<CasesOptions> options
        ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// In the case of a non admin user. Apply extra Where clauses to the IQueryable based on their roles.
    /// </summary>
    /// <param name="cases"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> cases, ClaimsPrincipal user) => Task.FromResult(cases);
    

    /// <summary>Determines whether user can see a Case in relation to i) user's role(s) and ii) case's CaseType and CheckpointType</summary>
    /// <param name="user"></param>
    /// <param name="case"></param>
    public async Task<bool> IsMember(ClaimsPrincipal user, Case @case) {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (@case is null) throw new ArgumentNullException(nameof(@case));

        // if client is systemic, then bypass checks
        if ((user.HasClaim(BasicClaimTypes.Scope, _options.RequiredScope ?? CasesCoreConstants.DefaultScopeName) && user.IsSystemClient()) || user.IsAdmin() || IsOwnerOfCase(user, @case)) {
            return true;
        }
        var userId = user.FindSubjectIdOrClientId();
        var roles = user.GetUserRoles();
        var accessRules = await GetAccessRules();

        var appliedRules = accessRules.Where(x => x.RuleCaseTypeId == @case.CaseType!.Id || x.RuleCheckpointTypeId == @case.CheckpointType.Id);
        if (!appliedRules.Any()) {
            return await _dbContext.CaseAccessRules
                .AsQueryable()
                .Where(x => x.RuleCaseId == @case.Id)
                .AnyAsync(x =>
                  roles.Contains(x.MemberRole!) ||
                  x.MemberUserId == userId ||
                  x.MemberGroupId == @case.GroupId
                );
        }

        return appliedRules.Any( x => roles.Contains(x.MemberRole!) || x.MemberUserId == userId || x.MemberGroupId == @case.GroupId);
    }

    /// <summary>Determines whether user is Owner of a Case</summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    private bool IsOwnerOfCase(ClaimsPrincipal user, Case @case) =>
        user.FindSubjectId()?.Equals(@case.CreatedById) == true;

    /// <summary>Gets the list of Members</summary>
    private async Task<List<AccessRule>> GetAccessRules() {
        return await _distributedCache.TryGetAndSetAsync(
            cacheKey: $"{MembersCacheKey}",
            getSourceAsync: async () => await _dbContext.CaseAccessRules
                .AsQueryable()
                .Where(x => x.RuleCaseTypeId.HasValue || x.RuleCheckpointTypeId.HasValue)
                .Select(x => new AccessRule { 
                    Id = x.Id,
                    AccessLevel = x.AccessLevel,
                    MemberGroupId = x.MemberGroupId,
                    MemberRole = x.MemberRole,
                    MemberUserId = x.MemberUserId,
                    RuleCaseId = x.RuleCaseId,
                    RuleCaseTypeId = x.RuleCaseTypeId,
                    RuleCheckpointTypeId = x.RuleCheckpointTypeId
                })
                .ToListAsync(),
            options: new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
    }


    private List<string> GetAllowedCheckpointTypes(List<string> roleClaims, List<Member> members) {
        // what CheckpointTypes can the user see based on their ROLE(S)?
        return members
            .Where(members => roleClaims.Contains(members.RoleName!))
            .Select(members => members.CheckpointTypeId.ToString())
            .Distinct() // Avoid duplicates: it is possible that user has >1 roles and those roles can "see" common CheckpointTypes
            .ToList();
    }
    private List<string> GetAllowedCaseTypeCodes(List<string> roleClaims, List<Member> members) {
        // what CaseType codes can the user see based on their ROLE(S)?
        return members
            .Where(members => roleClaims.Contains(members.RoleName!))
            .Select(x => x.CaseTypePartial!.Code)
            .Distinct()
            .ToList();
    }
}