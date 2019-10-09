using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="IDistributedCache"/>.
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.
        /// </summary>
        /// <typeparam name="T">The type of item expected from the cache.</typeparam>
        /// <param name="cache">The instance of distributed cache service.</param>
        /// <param name="cacheKey">The cache key to search for.</param>
        /// <param name="getSourceAsync">The delegate to use in order to retrieve the item if not found in cache.</param>
        /// <param name="options">The cache options to use when adding items to the cache.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>The item found in the cache under the specified key.</returns>
        public static async Task<T> TryGetOrSetAsync<T>(this IDistributedCache cache, string cacheKey, Func<Task<T>> getSourceAsync, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default) {
            var itemJson = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(itemJson)) {
                return JsonConvert.DeserializeObject<T>(itemJson);
            }
            var result = await getSourceAsync();
            if (result == null) {
                return await Task.FromResult(default(T));
            }
            itemJson = JsonConvert.SerializeObject(result);
            await cache.SetStringAsync(cacheKey, itemJson, options, cancellationToken);
            return result;
        }

        /// <summary>
        /// Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.
        /// </summary>
        /// <typeparam name="T">The type of item expected from the cache.</typeparam>
        /// <param name="cache">The instance of distributed cache service.</param>
        /// <param name="cacheKey">The cache key to search for.</param>
        /// <param name="getSource">The delegate to use in order to retrieve the item if not found in cache.</param>
        /// <param name="options">The cache options to use when adding items to the cache.</param>
        /// <returns>The item found in the cache under the specified key.</returns>
        public static T TryGetOrSet<T>(this IDistributedCache cache, string cacheKey, Func<T> getSource, DistributedCacheEntryOptions options) {
            var itemJson = cache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(itemJson)) {
                return JsonConvert.DeserializeObject<T>(itemJson);
            }
            var result = getSource();
            if (result == null) {
                return default;
            }
            itemJson = JsonConvert.SerializeObject(result);
            cache.SetString(cacheKey, itemJson, options);
            return result;
        }

        /// <summary>
        /// Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.
        /// </summary>
        /// <typeparam name="T">The type of item expected from the cache.</typeparam>
        /// <param name="cache">The instance of distributed cache service.</param>
        /// <param name="cacheKey">The cache key to search for.</param>
        /// <param name="getSourceAsync">The delegate to use in order to retrieve the item if not found in cache.</param>
        /// <param name="absoluteExpiration">The expiration timespan used to keep the item in the cache. If not provided, 1 hour is used by default.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>The item found in the cache under the specified key.</returns>
        public static async Task<T> TryGetOrSetAsync<T>(this IDistributedCache cache, string cacheKey, Func<Task<T>> getSourceAsync, TimeSpan? absoluteExpiration, CancellationToken cancellationToken = default) {
            return await cache.TryGetOrSetAsync(cacheKey, getSourceAsync, new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(absoluteExpiration ?? TimeSpan.FromHours(1))
            }, cancellationToken);
        }

        /// <summary>
        /// Tries to retrieve an item from the cache by using a unique key. If the item is not found, the provided source is used and the item is then saved in the cache.
        /// </summary>
        /// <typeparam name="T">The type of item expected from the cache.</typeparam>
        /// <param name="cache">The instance of distributed cache service.</param>
        /// <param name="cacheKey">The cache key to search for.</param>
        /// <param name="getSource">The delegate to use in order to retrieve the item if not found in cache.</param>
        /// <param name="absoluteExpiration">The expiration timespan used to keep the item in the cache. If not provided, 1 hour is used by default.</param>
        /// <returns>The item found in the cache under the specified key.</returns>
        public static T TryGetOrSet<T>(this IDistributedCache cache, string cacheKey, Func<T> getSource, TimeSpan? absoluteExpiration) {
            return cache.TryGetOrSet(cacheKey, getSource, new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(absoluteExpiration ?? TimeSpan.FromHours(1))
            });
        }
    }
}
