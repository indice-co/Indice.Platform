using System.Linq.Expressions;
using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
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
    public Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> casesQuery, WorkflowActor user) => Task.FromResult(casesQuery);


    /// <summary>Determines whether user can see a Case in relation to i) user's role(s) and ii) case's CaseType and CheckpointType</summary>
    public async Task<bool> IsMember(WorkflowActor user, Case @case) {
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
    private static bool IsOwnerOfCase(WorkflowActor user, Case @case) =>
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