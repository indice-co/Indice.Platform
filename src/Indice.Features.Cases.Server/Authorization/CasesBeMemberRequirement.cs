using Indice.Features.Cases.Core.Data;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

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
    public CasesBeMemberRequirement() {
    }
}



/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Admins, Users with admin role and user that can view a case based on Access Rules.</summary>
public class CasesBeMemberHandler : AuthorizationHandler<CasesBeMemberRequirement>, IAuthorizationRequirement
{
    private readonly CasesDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CasesBeMemberHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    /// <summary>
    /// Creates a new instance of <see cref="CasesBeMemberHandler"/>.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    /// <param name="httpContextAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesBeMemberHandler(
        CasesDbContext dbContext, 
        IDistributedCache cache, 
        ILogger<CasesBeMemberHandler> logger, 
        IHttpContextAccessor httpContextAccessor) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesBeMemberRequirement requirement) {
        var routeData = _httpContextAccessor.HttpContext.GetRouteData();
        if (!Guid.TryParse((string?)routeData.Values["policyId"], out var policyId)) {
            // If you cannot determine if requirement succeeded or not, please do nothing.
            return;
        }
        // Get user id/application id from the corresponding claims.
        var userId = context.User.FindFirstValue(JwtClaimTypes.Subject);
        var applicationId = context.User.FindFirstValue(JwtClaimTypes.ClientId);
        var memberId = string.IsNullOrEmpty(userId) ? applicationId : userId;
        var isMember = //context.User.IsVendor()
                       context.User.IsSystemClient()
                    || await CheckMembershipAsync(memberId, policyId, requirement.Level);
        // Apparently nothing else worked.
        if (!isMember) {
            _logger.LogInformation("Member {memberId} does not have role {requirementLevel}.", memberId, requirement.Level);
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

