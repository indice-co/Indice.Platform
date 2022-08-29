using Indice.AspNetCore.MultiTenancy;
using Indice.MultitenantApi.Data;
using Indice.MultitenantApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.MultitenantApi.Services
{
    public class SaasTenantStore : ITenantStore<Tenant>
    {
        private readonly SaasDbContext _dbContext;

        public SaasTenantStore(SaasDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<int?> GetAccessLevelAsync(Guid tenantId, string userId) => 
            await _dbContext.SubscriptionMembers
                            .Where(member => member.MemberId == Guid.Parse(userId) && member.SubscriptionId == tenantId)
                            .Select(member => (int?)member.AccessLevel)
                            .SingleOrDefaultAsync();

        public async Task<Tenant> GetTenantAsync(string identifier) {
            return await _dbContext
                .Subscriptions
                .Where(subscription => subscription.Alias == identifier.ToLowerInvariant() && subscription.Status == SubscriptionStatus.Enabled)
                .Select(subscription => new Tenant {
                    Id = subscription.Id,
                    Identifier = subscription.Alias,
                    ConnectionString = subscription.DatabaseConnection
                })
                .SingleOrDefaultAsync();
        }
    }
}
