using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
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
        private readonly string[] _dependentPaths;
        private readonly string[] _dependentStaticPaths;
        private string _cacheKey;

        public CacheResourceFilter(IDistributedCache cache, IOptions<MvcNewtonsoftJsonOptions> jsonOptions, string[] dependentPaths, string[] dependentStaticPaths) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _jsonSerializerSettings = jsonOptions?.Value?.SerializerSettings ?? throw new ArgumentNullException(nameof(jsonOptions));
            _dependentPaths = dependentPaths ?? Array.Empty<string>();
            _dependentStaticPaths = dependentStaticPaths ?? Array.Empty<string>();
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
            var basePath = string.Empty;
            if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor) {
                var isCacheable = actionDescriptor.MethodInfo.GetCustomAttributes(inherit: false).Count(x => x.GetType() == typeof(NoCacheAttribute)) == 0;
                var controllerRoute = actionDescriptor.ControllerTypeInfo.GetCustomAttributes(inherit: false).SingleOrDefault(x => x.GetType() == typeof(RouteAttribute));
                if (controllerRoute != null) {
                    basePath = $"/{(controllerRoute as RouteAttribute).Template}";
                }
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
                        _cache.SetString(_cacheKey, JsonConvert.SerializeObject(result.Value, _jsonSerializerSettings), new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                    }
                }
                if (requestMethod == HttpMethod.Post.Method || requestMethod == HttpMethod.Put.Method || requestMethod == HttpMethod.Patch.Method || requestMethod == HttpMethod.Delete.Method) {
                    _cache.Remove(_cacheKey);
                    foreach (var path in _dependentPaths) {
                        var dependentKey = $"{basePath}/{path}";
                        var regex = new Regex("{(.*?)}");
                        var match = regex.Match(path);
                        if (!match.Success) {
                            continue;
                        }
                        var routeValue = request.RouteValues.SingleOrDefault(x => $"{{{x.Key}}}" == match.Value);
                        if (routeValue.Value == null) {
                            continue;
                        }
                        dependentKey = dependentKey.Replace(match.Value, routeValue.Value.ToString());
                        var nextMatch = match.NextMatch();
                        var hasNextMatch = nextMatch.Success;
                        while (hasNextMatch) {
                            routeValue = request.RouteValues.SingleOrDefault(x => $"{x.Key}" == nextMatch.Value);
                            dependentKey = dependentKey.Replace(match.Value, routeValue.Value.ToString());
                            nextMatch = nextMatch.NextMatch();
                            hasNextMatch = nextMatch.Success;
                        }
                        _cache.Remove(dependentKey);
                    }
                    foreach (var path in _dependentStaticPaths) {
                        _cache.Remove(path.StartsWith("/") ? path : $"/{path}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// An attribute that is used to indicate that a controller or method should not cache the response.
    /// </summary>
    /// <remarks>Do not use this attribute together with <see cref="CacheResourceFilterAttribute"/> since the latter will have no effect.</remarks>
    [AttributeUsage(validOn: AttributeTargets.Method)]
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
        /// <param name="dependentPaths">Parent paths of the current method that must be invalidated. Path template variables must match by name.</param>
        /// <param name="dependentStaticPaths">Dependent static path that must be invalidated along with this resource.</param>
        public CacheResourceFilterAttribute(string[] dependentPaths = null, string[] dependentStaticPaths = null) : base(typeof(CacheResourceFilter)) {
            Arguments = new object[] {
                dependentPaths ?? Array.Empty<string>() ,
                dependentStaticPaths ?? Array.Empty<string>()
            };
        }
    }
}
