using Indice.AspNetCore.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Services;

/// <summary>A simple service class for managing cache operations for <see cref="CacheResourceFilter"/>.</summary>
public class CacheResourceKeysManager
{
    private static readonly HashSet<string> _cacheKeys = new();
    private readonly IDistributedCache _cache;

    /// <summary></summary>
    /// <param name="cache"></param>
    public CacheResourceKeysManager(IDistributedCache cache) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>Cache keys used internally by the <see cref="CacheResourceFilter"/></summary>
    public static HashSet<string> CacheKeys => _cacheKeys;

    /// <summary>Gets a string from the specified cache with the specified key.</summary>
    /// <param name="cacheKey">The key to get the stored data for.</param>
    /// <returns>The string value from the stored cache key.</returns>
    public string GetString(string cacheKey) => _cache.GetString(cacheKey);

    /// <summary>Sets a string in the specified cache with the specified key.</summary>
    /// <param name="cacheKey">The key to store the data in.</param>
    /// <param name="value">The data to store in the cache.</param>
    /// <param name="distributedCacheEntryOptions">Provides the cache options for an entry in <see cref="IDistributedCache"/>.</param>
    public void SetString(string cacheKey, string value, DistributedCacheEntryOptions distributedCacheEntryOptions) {
        _cache.SetString(cacheKey, value, distributedCacheEntryOptions);
        _cacheKeys.Add(cacheKey);
    }

    /// <summary>Removes the value with the given key.</summary>
    /// <param name="cacheKey">A string identifying the requested value.</param>
    public void Remove(string cacheKey) {
        _cache.Remove(cacheKey);
        _cacheKeys.Remove(cacheKey);
    }

    /// <summary>Removes all values.</summary>
    public void RemoveAll() {
        foreach (var key in _cacheKeys) {
            _cache.Remove(key);
        }
    }
}
