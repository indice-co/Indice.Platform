using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.MultiTenancy.Authorization
{
    /// <summary>
    /// This authorization requirement specifies that an endpoint must be 
    /// available only to members. Furthermore, members must have at leat a specific access level and above.
    /// </summary>
    public class BeTenantMemberRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The policy name corresponding to this requirement
        /// </summary>
        public const string PolicyName = "BeTenantMember";

        /// <summary>
        /// constructs the requirement for a minimum level
        /// </summary>
        /// <param name="minimumLevel"></param>
        public BeTenantMemberRequirement(int? minimumLevel = null) {
            Level = minimumLevel.GetValueOrDefault();
        }

        /// <summary>
        /// The minimum access Level requirement. Zero means plain member (default)
        /// </summary>
        public int Level { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(BeTenantMemberRequirement)}: Requires access Level '{Level}' for the current user or client.";
    }

    /// <summary>
    /// Authorization handler corresponding to the <see cref="BeTenantMemberRequirement"/> 
    /// </summary>
    /// <typeparam name="TTenant"></typeparam>
    public class BeTenantMemberHandler<TTenant> : AuthorizationHandler<BeTenantMemberRequirement> where TTenant : Tenant
    {
        private readonly ITenantStore<TTenant> _tenantStore;
        private readonly ITenantAccessor<TTenant> _tenantAccessor;
        private readonly IDistributedCache _cache;
        private readonly ILogger<BeTenantMemberHandler<TTenant>> _logger;

        /// <summary>
        /// Constructs the handler.
        /// </summary>
        /// <param name="tenantStore"></param>
        /// <param name="tenantAccessor"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        public BeTenantMemberHandler(ITenantStore<TTenant> tenantStore, ITenantAccessor<TTenant> tenantAccessor, IDistributedCache cache, ILogger<BeTenantMemberHandler<TTenant>> logger) {
            _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
            _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BeTenantMemberRequirement requirement) {
            var userIsAnonymous =
               context.User?.Identity == null ||
               !context.User.Identities.Any(i => i.IsAuthenticated);
            if (userIsAnonymous) {
                return;
            }
            var userId = context.User.FindFirstValue(JwtClaimTypesInternal.Subject);
            if (_tenantAccessor.Tenant is null) {
                // If you cannot determine if requirement succeeded or not, please do nothing.
                return;
            }
            // Get user id/application id from the corresponding claims.
            var memberId = userId;
            var isMember = context.User.IsSystemClient() || await CheckMembershipAsync(memberId, _tenantAccessor.Tenant.Id, requirement.Level);
            // Apparently nothing else worked.
            if (!isMember) {
                _logger.LogInformation($"Member {memberId} does not have role {requirement.Level}.");
                context.Fail();
            } else {
                context.Succeed(requirement);
            }
        }

        private async Task<bool> CheckMembershipAsync(string memberId, Guid tenantId, int? level) {
            var isMember = false;
            var cacheKey = $"m-{memberId}-t-{tenantId}-l-{level}";
            var value = await _cache.GetStringAsync(cacheKey);
            var entryExists = value != null;
            if (entryExists) {
                bool.TryParse(value, out isMember);
                return isMember;
            }
            // This is the case that cache is unavalable or this is the first authorization call for this requirement/policy.
            isMember = await _tenantStore.CheckAccessAsync(tenantId, memberId, level);
            // Add to cache. 
            var cacheEntryOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
            await _cache.SetStringAsync(cacheKey, $"{isMember}", cacheEntryOptions);
            return isMember;
        }
    }
}
