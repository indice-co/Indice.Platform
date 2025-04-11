using System.Runtime.InteropServices;
using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Authorization;



/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Admins, Users with admin role and user that can view a case based on Access Rules.</summary>
public class CasesAccessMemberHandler : AuthorizationHandler<CasesRecordsAccessLevelRequirement>, IAuthorizationRequirement {

    private readonly IDistributedCache _cache;
    private readonly ILogger<CasesAccessMemberHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CasesOptions _casesOptions;
    private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;
    /// <summary>
    /// Creates a new instance of <see cref="CasesAccessOwnerHandler"/>.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="casesOptions"></param>
    /// <param name="logger"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="memberAuthorizationProvider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesAccessMemberHandler(
        IDistributedCache cache,
        IOptions<CasesOptions> casesOptions,
        ILogger<CasesAccessMemberHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        ICaseAuthorizationProvider memberAuthorizationProvider) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _casesOptions = casesOptions?.Value ?? throw new ArgumentNullException(nameof(casesOptions));
        _memberAuthorizationProvider = memberAuthorizationProvider;
    }

    /// <summary>Creates a new instance of <see cref="CasesAccessRoleBasedHandler"/>.</summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesRecordsAccessLevelRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            _logger.LogInformation("Request is unauthorized.");
            return;
        }

        var allowedUser = !string.IsNullOrEmpty(context.User!.FindSubjectId()) &&
            (context.User!.IsAdmin() || context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator));

        var allowedClient = string.IsNullOrEmpty(context.User!.FindSubjectId());

        if (allowedUser || allowedClient) {
            context.Succeed(requirement);
            return;
        }

        var actor = context.User!.UserToActor(_casesOptions);
        var routeData = _httpContextAccessor.HttpContext!.GetRouteData();
        if (!Guid.TryParse((string?)routeData.Values["caseId"], out var caseId)) {
            // If you cannot determine if requirement succeeded or not, please do nothing.
            caseId = Guid.Empty;
        }

        var isMember = await CheckMembershipAsync(actor, caseId, requirement);
        if (!isMember) {
            _logger.LogInformation("User {UserId} is not a member.", actor.Id);

        } else {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> CheckMembershipAsync(WorkflowActor actor, Guid caseId, CasesRecordsAccessLevelRequirement requirement) {
        
        var cacheKey = $"member:{actor.Id}-caseId:{caseId}-level:{requirement.MinimumAccessLevel}";
        var value = await _cache.GetStringAsync(cacheKey);
        var entryExists = value != null;
        if (entryExists && int.TryParse(value, out var accessLevel)) {
            
            return accessLevel >= requirement.MinimumAccessLevel.GetHashCode();
        }
        accessLevel = await _memberAuthorizationProvider.MemberAccess(actor, caseId);
        // Add to cache. 
        var cacheEntryOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
        await _cache.SetStringAsync(cacheKey, $"{accessLevel}", cacheEntryOptions);
        return accessLevel >= requirement.MinimumAccessLevel.GetHashCode();
    }

}


