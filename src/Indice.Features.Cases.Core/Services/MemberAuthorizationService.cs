using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

internal class MemberAuthorizationService : ICaseAuthorizationService
{
    private readonly CasesDbContext _dbContext;
    private readonly IDistributedCache _distributedCache;
    private const string MembersCacheKey = $"{nameof(MemberAuthorizationService)}.members";

    /// <summary>
    /// A service that determines which cases can the user access based on their role.
    /// </summary>
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
    public Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> casesQuery, UserActor user) => Task.FromResult(casesQuery);


    /// <summary>Determines whether user can see a Case in relation to i) user's role(s) and ii) case's CaseType and CheckpointType</summary>
    public async Task<bool> IsMember(UserActor user, Case @case) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(@case);

        // if client is systemic, then bypass checks
        if (user.IsSystemClient || user.IsAdmin || IsOwnerOfCase(user, @case)) {
            return true;
        }

        var accessRules = await GetAccessRules();
        var hasAccessBasedOnCaseTypeOrCheckpoint = accessRules
                                    .Where(x => x.RuleCaseTypeId == @case.CaseType!.Id || x.RuleCheckpointTypeId == @case.CheckpointType.Id)
                                    .Any(x => user.Roles.Contains(x.MemberRole!) || x.MemberUserId == user.Id || x.MemberGroupId == @case.GroupId);
        if (hasAccessBasedOnCaseTypeOrCheckpoint) {
            return true;
        }
        var accessPredicate = DbCaseAccessRule.AccessMatchPredicate(user.Id, user.Roles, user.GroupId);
        var rulePredicate = DbCaseAccessRule.RuleMatchPredicate(@case.Id, @case.CaseType!.Id, @case.CheckpointType.Id);
        return await _dbContext.CaseAccessRules
                .AsNoTracking()
                .Where(rulePredicate)
                .Where(accessPredicate)
                .AnyAsync();
    }

    /// <summary>Determines whether user is Owner of a Case</summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    private static bool IsOwnerOfCase(UserActor user, Case @case) =>
        user.Id?.Equals(@case.CreatedById) == true;

    /// <summary>Gets the list of Members</summary>
    private async Task<List<AccessRule>> GetAccessRules() {
        return (await _distributedCache.TryGetAndSetAsync(
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
            }))!;
    }

    public async Task<int> MemberAccess(UserActor user, Guid caseId) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);

        var dbcase = await _dbContext.Cases
                .AsNoTracking()
                .Include(x => x.Checkpoint)
                .FirstOrDefaultAsync(x => x.Id == caseId);

        if (dbcase == null) { return -1; }
        // Create a case details just for the authorization, with the min required properties
        var accessPredicate = DbCaseAccessRule.AccessMatchPredicate(user.Id, user.Roles, user.GroupId);
        var rulePredicate = DbCaseAccessRule.RuleMatchPredicate(dbcase.Id, dbcase.CaseTypeId, dbcase.Checkpoint.CheckpointTypeId);
        return await _dbContext.CaseAccessRules
                .Where(rulePredicate)
                .Where(accessPredicate)
                .Select(x => x.AccessLevel)
                .MaxAsync();
    }
}