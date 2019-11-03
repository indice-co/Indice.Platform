using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// A resource filter used to short-circuit most of the pipeline if the response is already cached.
    /// </summary>
    internal class CacheResourceFilter : IResourceFilter
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private string _cacheKey;

        public CacheResourceFilter(IDistributedCache cache, IOptions<MvcNewtonsoftJsonOptions> jsonOptions) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _jsonSerializerSettings = jsonOptions?.Value?.SerializerSettings ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        public void OnResourceExecuting(ResourceExecutingContext context) {
            _cacheKey = context.HttpContext.Request.Path.ToString();
            var cachedValue = _cache.GetString(_cacheKey);
            if (!string.IsNullOrEmpty(cachedValue)) {
                context.Result = new OkObjectResult(JObject.Parse(cachedValue));
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context) {
            if (!string.IsNullOrEmpty(_cacheKey)) {
                var cachedValue = _cache.GetString(_cacheKey);
                if (string.IsNullOrEmpty(cachedValue) && context.Result is OkObjectResult result) {
                    _cache.SetString(_cacheKey, JsonConvert.SerializeObject(result.Value, _jsonSerializerSettings));
                }
            }
        }
    }

    /// <summary>
    /// A type filter attribute for <see cref="CacheResourceFilter"/>.
    /// </summary>
    public sealed class CacheResourceFilterAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="CacheResourceFilterAttribute"/>.
        /// </summary>
        public CacheResourceFilterAttribute() : base(typeof(CacheResourceFilter)) { }
    }
}
