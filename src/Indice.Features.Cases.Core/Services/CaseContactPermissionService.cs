using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Cases.Core.Services;

internal class CaseContactPermissionService : ICaseContactPermissionService
{
    /// <summary>The cached value for a customer that takes no part in the case.</summary>
    private const string NoAccess = "none";
    // Absolute, so an entry that an invalidation could not reach (a cache that is not shared between instances) expires even while it is in use.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly CasesDbContext _dbContext;
    private readonly IDistributedCache _cache;

    public CaseContactPermissionService(CasesDbContext dbContext, IDistributedCache cache) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public async Task<ContactPermission?> GetPermission(string userId, Guid caseId) {
        if (string.IsNullOrEmpty(userId)) {
            return null;
        }
        var cacheKey = GetCacheKey(userId, caseId);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null) {
            return Enum.TryParse<ContactPermission>(cached, out var cachedPermission) && Enum.IsDefined(cachedPermission) ? cachedPermission : null;
        }
        var access = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.Id == caseId)
            .Select(c => new {
                IsOwner = c.Owner.UserId == userId,
                OwnerPermission = c.Owner.Permission,
                IsCreator = c.CreatedBy.Id == userId,
                CoOwnerPermission = c.CoOwners
                    .Where(x => x.CoOwner.UserId == userId)
                    .Select(x => (ContactPermission?)x.CoOwner.Permission)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
        if (access is null) {
            // The case does not exist. Nothing to cache.
            return null;
        }
        // The owner is checked first, so a lowered owner permission cannot be bypassed by also being the creator.
        ContactPermission? permission = access.IsOwner
            ? access.OwnerPermission
            : access.CoOwnerPermission ?? (access.IsCreator ? DbCase.CreatorPermission : null);
        await _cache.SetStringAsync(cacheKey, permission?.ToString() ?? NoAccess, new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });
        return permission;
    }

    /// <inheritdoc />
    public Task Invalidate(string userId, Guid caseId) =>
        string.IsNullOrEmpty(userId) ? Task.CompletedTask : _cache.RemoveAsync(GetCacheKey(userId, caseId));

    /// <summary>One key per user and case, so a change can be invalidated without knowing which permission was asked for.</summary>
    private static string GetCacheKey(string userId, Guid caseId) => $"case-contact:{caseId:N}:{userId}";
}
