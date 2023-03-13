using Indice.Features.Multitenancy.Core;
using Indice.Sample.Common.Data;
using Indice.Sample.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Sample.Common.Services;

public class SaasTenantStore : ITenantStore<ExtendedTenant>
{
    private readonly SaasDbContext _dbContext;

    public SaasTenantStore(SaasDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int?> GetAccessLevelAsync(Guid tenantId, string userId) {
        var accessLevel = await _dbContext
            .SubscriptionMembers
            .Where(member => member.MemberId == Guid.Parse(userId) && member.SubscriptionId == tenantId)
            .Select(member => (int?)member.AccessLevel)
            .SingleOrDefaultAsync();
        return accessLevel;
    }

    public async Task<ExtendedTenant> GetTenantAsync(string identifier) {
        var tenant = await _dbContext
            .Subscriptions
            .Where(subscription => subscription.Alias == identifier.ToLowerInvariant() && subscription.Status == SubscriptionStatus.Enabled)
            .Select(subscription => new ExtendedTenant {
                Id = subscription.Id,
                Identifier = subscription.Alias,
                ConnectionString = subscription.DatabaseConnectionString,
                PushNotificationConnectionString = subscription.PushNotificationsConnectionString
            })
            .SingleOrDefaultAsync();
        return tenant;
    }

    public async Task LogActivityAsync(Guid tenantId, string userId) {
        var member = await _dbContext
            .SubscriptionMembers
            .Where(member => member.MemberId == Guid.Parse(userId) && member.SubscriptionId == tenantId)
            .SingleOrDefaultAsync();
        member.LastAccessDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
