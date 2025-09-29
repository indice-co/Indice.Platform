#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides a hybrid cache implementation for storing and retrieving objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of object to cache.</typeparam>
public class HybridCacheStore<T> : ICache<T> where T : class
{
    private readonly HybridCache _distributedCache;
    private readonly string _prefix;

    /// <inheritdoc />
    public HybridCacheStore(HybridCache distributedCache) {
        _distributedCache = distributedCache;
        _prefix = typeof(T).FullName ?? typeof(T).Name;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync(string key) {
        var cacheKey = $"{_prefix}:{key}";
        var result = await _distributedCache.GetOrCreateAsync<T>(cacheKey, _ => {
            // nothing to load here since Duende will call SetAsync when needed
            return default;
        });
        return result;
    }

    /// <inheritdoc />
    public async Task<T> GetOrAddAsync(string key, TimeSpan duration, Func<Task<T>> get) {
        var cacheKey = $"{_prefix}:{key}";
        var result = await _distributedCache.GetOrCreateAsync<T>(
            cacheKey,
            async ct => await get(),
            new HybridCacheEntryOptions { Expiration = duration }
        );
        return result;
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, T item, TimeSpan expiration) {
        var cacheKey = $"{_prefix}:{key}";
        await _distributedCache.SetAsync(
            cacheKey,
            item,
            new HybridCacheEntryOptions { Expiration = expiration }
        );
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key) {
        var cacheKey = $"{_prefix}:{key}";
        await _distributedCache.RemoveAsync(cacheKey);
    }
}

#endif