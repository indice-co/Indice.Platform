using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Services;

internal class MemberAuthorizationService : ICaseAuthorizationService
{
    private readonly CasesDbContext _dbContext;
    private readonly IDistributedCache _distributedCache;
    private readonly AdminCasesApiOptions _options;
    private const string MembersCacheKey = $"{nameof(MemberAuthorizationService)}.members";

    /// <summary>
    /// A service that determines which cases can the user access based on their role.
    /// </summary>
    public MemberAuthorizationService(
        CasesDbContext dbContext,
        IDistributedCache distributedCache,
        IOptions<AdminCasesApiOptions> options
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
    public async Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> cases, ClaimsPrincipal user) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        if ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin()) {
            return cases;
        }

        // if the user is not systemic or admin then based on their role they have access to specific checkpoint types and case type codes.
        var roleClaims = GetUserRoles(user);
        var members = await GetMembers();

        if (GetAllowedCheckpointTypes(roleClaims, members) is not { } allowedCheckpointTypeIds ||
            GetAllowedCaseTypeCodes(roleClaims, members) is not { } allowedCaseTypeCodes) {
            // if the (non-admin) user comes with no available caseTypes or CheckpointTypes to see, tough luck!
            throw new ResourceUnauthorizedException("User does not has access to cases");
        }

        if (allowedCheckpointTypeIds.Any()) {
            cases = cases.Where(cp => allowedCheckpointTypeIds.Contains(cp.CheckpointType.Id.ToString()));
        }
        if (allowedCaseTypeCodes.Any()) {
            cases = cases.Where(cp => allowedCaseTypeCodes.Contains(cp.CaseType.Code));
        }
        return cases;
    }

    /// <summary>Determines whether user can see a Case in relation to i) user's role(s) and ii) case's CaseType and CheckpointType</summary>
    /// <param name="user"></param>
    /// <param name="case"></param>
    public async Task<bool> IsMember(ClaimsPrincipal user, Case @case) {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (@case is null) throw new ArgumentNullException(nameof(@case));

        // if client is systemic, then bypass checks
        if ((user.HasClaim(BasicClaimTypes.Scope, _options.ExpectedScope ?? CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin() || IsOwnerOfCase(user, @case)) {
            return true;
        }

        var roles = GetUserRoles(user);
        var members = await GetMembers();

        var memberships = members
            .Where(c => roles.Contains(c.RoleName!))
            .ToList();

        return memberships
            .Any(x => x.CaseTypeId == @case.CaseType!.Id && x.CheckpointTypeId == @case.CheckpointType.Id);
    }

    /// <summary>Determines whether user is Owner of a Case</summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    private bool IsOwnerOfCase(ClaimsPrincipal user, Case @case) =>
        user.FindSubjectId().Equals(@case.CreatedById);

    /// <summary>Gets the list of Members</summary>
    private async Task<List<Member>> GetMembers() {
        return await _distributedCache.TryGetAndSetAsync(
            cacheKey: $"{MembersCacheKey}",
            getSourceAsync: async () => await _dbContext.Members
                .AsQueryable()
                .Select(c => new Member {
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
            .Select(x => x.CaseTypePartial.Code)
            .Distinct()
            .ToList();
    }
}