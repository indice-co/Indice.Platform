using System.Text.Json;

namespace Microsoft.Extensions.Caching.Distributed;

/// <summary>Extension methods on <see cref="IDistributedCache"/>.</summary>
public static class IDistributedCacheExtensions
{
    /// <summary>Tries to retrieve an item from the cache by using a unique key.</summary>
    /// <typeparam name="T">The type of item expected from the cache.</typeparam>
    /// <param name="cache">The instance of distributed cache service.</param>
    /// <param name="cacheKey">The cache key to search for.</param>
    /// <param name="jsonSerializerOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The item found in the cache under the specified key.</returns>
    public static async Task<T> GetAsync<T>(this IDistributedCache cache, string cacheKey, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default) {
        var itemJson = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(itemJson)) {
            return JsonSerializer.Deserialize<T>(itemJson, jsonSerializerOptions);
        }
        return default;
    }

    /// <summary>Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.</summary>
    /// <typeparam name="T">The type of item expected from the cache.</typeparam>
    /// <param name="cache">The instance of distributed cache service.</param>
    /// <param name="cacheKey">The cache key to search for.</param>
    /// <param name="getSourceAsync">The delegate to use in order to retrieve the item if not found in cache.</param>
    /// <param name="options">The cache options to use when adding items to the cache.</param>
    /// <param name="jsonSerializerOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The item found in the cache under the specified key.</returns>
    public static async Task<T> TryGetAndSetAsync<T>(this IDistributedCache cache, string cacheKey, Func<Task<T>> getSourceAsync, DistributedCacheEntryOptions options, JsonSerializerOptions jsonSerializerOptions = null, CancellationToken cancellationToken = default) {
        var itemJson = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(itemJson)) {
            return JsonSerializer.Deserialize<T>(itemJson, jsonSerializerOptions);
        }
        var result = await getSourceAsync();
        if (result == null) {
            return await Task.FromResult(default(T));
        }
        itemJson = JsonSerializer.Serialize(result, jsonSerializerOptions);
        await cache.SetStringAsync(cacheKey, itemJson, options, cancellationToken);
        return result;
    }

    /// <summary>Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.</summary>
    /// <typeparam name="T">The type of item expected from the cache.</typeparam>
    /// <param name="cache">The instance of distributed cache service.</param>
    /// <param name="cacheKey">The cache key to search for.</param>
    /// <param name="getSourceAsync">The delegate to use in order to retrieve the item if not found in cache.</param>
    /// <param name="absoluteExpiration">The expiration timespan used to keep the item in the cache. If not provided, 1 hour is used by default.</param>
    /// <param name="jsonSerializerOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The item found in the cache under the specified key.</returns>
    public static async Task<T> TryGetAndSetAsync<T>(this IDistributedCache cache, string cacheKey, Func<Task<T>> getSourceAsync, TimeSpan? absoluteExpiration = null, JsonSerializerOptions jsonSerializerOptions = null, CancellationToken cancellationToken = default) =>
        await cache.TryGetAndSetAsync(cacheKey, getSourceAsync, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.Add(absoluteExpiration ?? TimeSpan.FromHours(1))
        }, jsonSerializerOptions, cancellationToken);

    /// <summary>Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.</summary>
    /// <typeparam name="T">The type of item expected from the cache.</typeparam>
    /// <param name="cache">The instance of distributed cache service.</param>
    /// <param name="cacheKey">The cache key to search for.</param>
    /// <param name="getSource">The delegate to use in order to retrieve the item if not found in cache.</param>
    /// <param name="options">The cache options to use when adding items to the cache.</param>
    /// <param name="jsonSerializerOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    /// <returns>The item found in the cache under the specified key.</returns>
    public static T TryGetAndSet<T>(this IDistributedCache cache, string cacheKey, Func<T> getSource, DistributedCacheEntryOptions options, JsonSerializerOptions jsonSerializerOptions = null) {
        var itemJson = cache.GetString(cacheKey);
        if (!string.IsNullOrEmpty(itemJson)) {
            return JsonSerializer.Deserialize<T>(itemJson, jsonSerializerOptions);
        }
        var result = getSource();
        if (result == null) {
            return default;
        }
        itemJson = JsonSerializer.Serialize(result, jsonSerializerOptions);
        cache.SetString(cacheKey, itemJson, options);
        return result;
    }

    /// <summary>Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.</summary>
    /// <typeparam name="T">The type of item expected from the cache.</typeparam>
    /// <param name="cache">The instance of distributed cache service.</param>
    /// <param name="cacheKey">The cache key to search for.</param>
    /// <param name="getSource">The delegate to use in order to retrieve the item if not found in cache.</param>
    /// <param name="absoluteExpiration">The expiration timespan used to keep the item in the cache. If not provided, 1 hour is used by default.</param>
    /// <param name="jsonSerializerOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    /// <returns>The item found in the cache under the specified key.</returns>
    public static T TryGetAndSet<T>(this IDistributedCache cache, string cacheKey, Func<T> getSource, TimeSpan? absoluteExpiration = null, JsonSerializerOptions jsonSerializerOptions = null) =>
        cache.TryGetAndSet(cacheKey, getSource, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.Add(absoluteExpiration ?? TimeSpan.FromHours(1))
        }, jsonSerializerOptions);
}
