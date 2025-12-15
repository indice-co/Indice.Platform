using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to case Owners.</summary>
public class CasesAccessOwnerHandler : AuthorizationHandler<CasesOwnerAccessRequirement>, IAuthorizationRequirement {
    private readonly IDistributedCache _cache;
    private readonly CasesDbContext dbContext;
    private readonly ILogger<CasesAccessOwnerHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CasesOptions _casesOptions;

    /// <summary>
    /// Creates a new instance of <see cref="CasesAccessOwnerHandler"/>.
    /// </summary>
    /// <param name="memberAuthorizationProvider"></param>
    /// <param name="cache"></param>
    /// <param name="casesOptions"></param>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    /// <param name="httpContextAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesAccessOwnerHandler(
        ICaseAuthorizationProvider memberAuthorizationProvider,
        IDistributedCache cache,
        IOptions<CasesOptions> casesOptions,
        CasesDbContext dbContext,
        ILogger<CasesAccessOwnerHandler> logger,
        IHttpContextAccessor httpContextAccessor) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.dbContext = dbContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _casesOptions = casesOptions?.Value ?? throw new ArgumentNullException(nameof(casesOptions));
    }

    /// <summary>Creates a new instance of <see cref="CasesAccessRoleBasedHandler"/>.</summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesOwnerAccessRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            _logger.LogInformation("Request is unauthorized.");
            return;
        }

        var actor = context.User!.UserToActor(_casesOptions);
        var routeData = _httpContextAccessor.HttpContext!.GetRouteData();
        if (!Guid.TryParse((string?)routeData.Values["caseId"], out var caseId)) {
            // If you cannot determine if requirement succeeded or not, please do nothing.
            return;
        }

        var isOwner = await CheckOwnershipAsync(actor, caseId);
        if (!isOwner) {
            _logger.LogInformation("User {UserId} is not a member.", actor.Id);

        } else {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> CheckOwnershipAsync(UserActor actor, Guid caseId) {
        var isOwner = false;
        var cacheKey = $"owner:{actor.Id}-caseId:{caseId}";
        var value = await _cache.GetStringAsync(cacheKey);
        var entryExists = value != null;
        if (entryExists) {
            bool.TryParse(value, out isOwner);
            return isOwner;
        }
        isOwner = await dbContext.Cases.AnyAsync(c => c.Id == caseId && (c.Owner.UserId == actor.Id || c.CreatedBy.Id == actor.Id));
        // Add to cache. 
        var cacheEntryOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(60));
        await _cache.SetStringAsync(cacheKey, $"{isOwner}", cacheEntryOptions);
        return isOwner;
    }
}