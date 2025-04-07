using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Integration;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Authorization;
/// <summary>
/// Represents a requirement for a user to be a member with a specific access level in order to access certain resources.
/// </summary>
public class CasesBeMemberRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = CaseServerConstants.Policies.BeCasesMember;

    /// <summary>
    /// Initializes a new instance of the <see cref="CasesBeMemberRequirement"/> class.
    /// </summary>
    public CasesBeMemberRequirement(CasesAccessLevel minimumAccessLevel = CasesAccessLevel.Member) {
        MinimumAccessLevel = minimumAccessLevel;
    }
    /// <summary>The minimum access level needed to access the protected resources</summary>
    public CasesAccessLevel MinimumAccessLevel { get; }
    
    /// <inheritdoc/>
    public override string ToString() => $"Requires Cases {MinimumAccessLevel} Access.";
}



/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Admins, Users with admin role and user that can view a case based on Access Rules.</summary>
public class CasesBeMemberHandler : AuthorizationHandler<CasesBeMemberRequirement>, IAuthorizationRequirement
{

    private readonly IDistributedCache _cache;
    private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;
    private readonly ILogger<CasesBeMemberHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CasesOptions _casesOptions;
    /// <summary>
    /// Creates a new instance of <see cref="CasesBeMemberHandler"/>.
    /// </summary>
    /// <param name="memberAuthorizationProvider"></param>
    /// <param name="cache"></param>
    /// <param name="casesOptions"></param>
    /// <param name="logger"></param>
    /// <param name="httpContextAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesBeMemberHandler(
        ICaseAuthorizationProvider memberAuthorizationProvider,
        IDistributedCache cache,
        IOptions<CasesOptions> casesOptions,
        ILogger<CasesBeMemberHandler> logger,
        IHttpContextAccessor httpContextAccessor) {
        _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _casesOptions = casesOptions?.Value ?? throw new ArgumentNullException(nameof(casesOptions));
    }



    /// <summary>Creates a new instance of <see cref="CasesAccessHandler"/>.</summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesBeMemberRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            _logger.LogInformation("Request is unauthorized.");
            context.Fail();
        }

        var actor = context.User!.UserToActor(_casesOptions);
        var allowedAccessLevel = 
            requirement.MinimumAccessLevel switch {
                CasesAccessLevel.Admin => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator),
                CasesAccessLevel.Manager => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator) || context.User!.HasRoleClaim(BasicRoleNames.CasesManager),
                CasesAccessLevel.Member => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator) || context.User!.HasRoleClaim(BasicRoleNames.CasesManager) || context.User!.HasRoleClaim(BasicRoleNames.CasesUser),
                _ => false
            };

        if (!allowedAccessLevel) {

            _logger.LogInformation("User {UserId} is not a member.", actor.Id);
            context.Fail();
        }

        var routeData = _httpContextAccessor.HttpContext!.GetRouteData();
        if (!Guid.TryParse((string?)routeData.Values["caseId"], out var caseId)) {
            // If you cannot determine if requirement succeeded or not, please do nothing.
            return;
        }

        var isMember = await _memberAuthorizationProvider.IsMember(actor, caseId);
        if (!isMember) {
            _logger.LogInformation("User {UserId} is not a member.", actor.Id);
            context.Fail();
        } else {
            context.Succeed(requirement);
        }
    }


    private async Task<bool> CheckMembershipAsync(string memberId, Guid policyId, CasesAccessLevel? level) {
        var hasMembership = false;
        var cacheKey = $"member-{memberId}-policy-{policyId}-level-{level}";
        var value = await _cache.GetStringAsync(cacheKey);
        //var entryExists = value != null;
        //if (entryExists) {
        //    bool.TryParse(value, out hasMembership);
        //    return hasMembership;
        //}
        //// This is the case that cache is unavalable or this is the first authorization call for this requirement/policy.
        //hasMembership = await _dbContext.PolicyMembers
        //                                .Where(s => s.MemberId == memberId && s.PolicyId == policyId && (level == null || s.AccessLevel >= level.Value))
        //                                .AnyAsync();
        // Add to cache. 
        var cacheEntryOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
        await _cache.SetStringAsync(cacheKey, $"{hasMembership}", cacheEntryOptions);
        return hasMembership;
    }
}

