using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
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
    internal sealed class CacheResourceFilter : IResourceFilter
    {
        private readonly IDistributedCache _cache;
        // Invoke the same JSON serializer settings that are used by the output formatter, so we can save objects in the cache in the exact same manner.
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private string _cacheKey;

        public CacheResourceFilter(IDistributedCache cache, IOptions<MvcNewtonsoftJsonOptions> jsonOptions) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _jsonSerializerSettings = jsonOptions?.Value?.SerializerSettings ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        /// <summary>
        /// Can run code before the rest of the filter pipeline. For example, <see cref="OnResourceExecuting(ResourceExecutingContext)"/> can run code before model binding.
        /// </summary>
        /// <param name="context">A context for resource filters, specifically <see cref="OnResourceExecuting(ResourceExecutingContext)"/> calls.</param>
        public void OnResourceExecuting(ResourceExecutingContext context) {
            var request = context.HttpContext.Request;
            _cacheKey = request.Path.ToString();
            var requestMethod = request.Method;
            var cachedValue = _cache.GetString(_cacheKey);
            // If there is a cached response for this path and the request method is of type 'GET', then break the pipeline and send the cached response.
            if (!string.IsNullOrEmpty(cachedValue) && requestMethod == HttpMethod.Get.Method) {
                context.Result = new OkObjectResult(JObject.Parse(cachedValue));
            }
        }

        /// <summary>
        /// Can run code after the rest of the pipeline has completed.
        /// </summary>
        /// <param name="context">A context for resource filters, specifically <see cref="OnResourceExecuted(ResourceExecutedContext)"/> calls.</param>
        public void OnResourceExecuted(ResourceExecutedContext context) {
            var request = context.HttpContext.Request;
            var requestMethod = request.Method;
            if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor) {
                var isCacheable = actionDescriptor.MethodInfo.GetCustomAttributes(inherit: false).Count(x => x.GetType() == typeof(NoCacheAttribute)) == 0;
                if (!isCacheable) {
                    return;
                }
            }
            // Check if the cache key has been properly set, which is crucial for interacting with the cache.
            if (!string.IsNullOrEmpty(_cacheKey)) {
                // If request method is of type 'GET' then we can cache the response.
                if (requestMethod == HttpMethod.Get.Method) {
                    var cachedValue = _cache.GetString(_cacheKey);
                    // Check if we already have a cached value for this cache key and also that response status code is 200 OK.
                    if (string.IsNullOrEmpty(cachedValue) && (context.Result is OkObjectResult result)) {
                        _cache.SetString(_cacheKey, JsonConvert.SerializeObject(result.Value, _jsonSerializerSettings));
                    }
                }
                if (requestMethod == HttpMethod.Post.Method || requestMethod == HttpMethod.Put.Method || requestMethod == HttpMethod.Patch.Method || requestMethod == HttpMethod.Delete.Method) {
                    _cache.Remove(_cacheKey);
                }
            }
        }
    }

    /// <summary>
    /// An attribute that is used to indicate that a controller or method should not cache the response.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Method)]
    /// <remarks>Do not use this attribute together with <see cref="CacheResourceFilterAttribute"/> since the latter will have no effect.</remarks>
    internal sealed class NoCacheAttribute : Attribute { }

    /// <summary>
    /// A type filter attribute for <see cref="CacheResourceFilter"/>.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters#typefilterattribute</remarks>
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CacheResourceFilterAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="CacheResourceFilterAttribute"/>.
        /// </summary>
        public CacheResourceFilterAttribute() : base(typeof(CacheResourceFilter)) { }
    }
}
