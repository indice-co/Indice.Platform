using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Indice.AspNetCore.Http.Filters;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Filters;

/// <summary>A resource filter used to short-circuit most of the pipeline if the response is already cached.</summary>
public sealed class CacheResourceFilter : IAsyncResourceFilter
{
    // Invoke the same JSON serializer settings that are used by the output formatter, so we can save objects in the cache in the exact same manner.
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly CacheResourceFilterOptions _cacheResourceFilterOptions;
    private readonly ICacheResourceFilterKeyExtensionResolver _keyExtensionResolver;
    private readonly CacheResourceKeysManager _cacheResourceKeysManager;
    private readonly string[] _dependentPaths;
    private readonly string[] _dependentStaticPaths;
    private readonly int _expiration;
    private readonly string[] _varyByClaimType;
    private string _cacheKey;

    /// <summary>Constructs a <see cref="CacheResourceFilter"/>.</summary>
    /// <param name="cacheResourceKeysManager">A simple service class for managing cache operations for <see cref="CacheResourceFilter"/>.</param>
    /// <param name="jsonOptions">Provides programmatic configuration for JSON in the MVC framework.</param>
    /// <param name="cacheResourceFilterOptions">Options for the <see cref="CacheResourceFilter"/>.</param>
    /// <param name="keyExtensionResolver">An optional extension to the cache key discriminator that will be created inside on th <see cref="CacheResourceFilter"/>.</param>
    /// <param name="dependentPaths">Parent paths of the current method that must be invalidated. Path template variables must match by name.</param>
    /// <param name="dependentStaticPaths">Dependent static path that must be invalidated along with this resource.</param>
    /// <param name="expiration">The absolute expiration in minutes of the cache item, expressed as an <see cref="int"/>. Defaults to 60 minutes.</param>
    /// <param name="varyByClaimType">The claim to use which value is included in the cache key.</param>
    public CacheResourceFilter(
        CacheResourceKeysManager cacheResourceKeysManager,
        IOptions<JsonOptions> jsonOptions,
        IOptions<CacheResourceFilterOptions> cacheResourceFilterOptions,
        IEnumerable<ICacheResourceFilterKeyExtensionResolver> keyExtensionResolver,
        string[] dependentPaths,
        string[] dependentStaticPaths,
        int expiration,
        string[] varyByClaimType
    ) {
        _jsonSerializerOptions = jsonOptions?.Value?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        _cacheResourceFilterOptions = cacheResourceFilterOptions?.Value ?? throw new ArgumentNullException(nameof(cacheResourceFilterOptions));
        _keyExtensionResolver = keyExtensionResolver?.FirstOrDefault(); // this is optional do not throw argument null exception!
        _cacheResourceKeysManager = cacheResourceKeysManager ?? throw new ArgumentNullException(nameof(cacheResourceKeysManager));
        _dependentPaths = dependentPaths ?? [];
        _dependentStaticPaths = dependentStaticPaths ?? [];
        _expiration = expiration;
        _varyByClaimType = varyByClaimType;
    }

    /// <summary>Can run code before the rest of the filter pipeline. For example, <see cref="OnResourceExecutingAsync(ResourceExecutingContext)"/> can run code before model binding.</summary>
    /// <param name="context">A context for resource filters, specifically <see cref="OnResourceExecutingAsync(ResourceExecutingContext)"/> calls.</param>
    public async Task OnResourceExecutingAsync(ResourceExecutingContext context) {
        if (_cacheResourceFilterOptions.DisableCache) {
            return;
        }
        var httpContext = context.HttpContext;
        var request = httpContext.Request;
        _cacheKey = $"{request.Path}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}";
        _cacheKey = await AddCacheKeyDiscriminatorAsync(context.HttpContext, _cacheKey);
        var requestMethod = request.Method;
        var cachedValue = _cacheResourceKeysManager.GetString(_cacheKey);
        // If there is a cached response for this path and the request method is of type 'GET', then break the pipeline and send the cached response.
        if (!string.IsNullOrEmpty(cachedValue) && (requestMethod == HttpMethod.Get.Method || requestMethod == HttpMethod.Head.Method)) {
            context.Result = new OkObjectResult(JsonDocument.Parse(cachedValue).RootElement);
        }
    }

    /// <summary>Can run code after the rest of the pipeline has completed.</summary>
    /// <param name="context">A context for resource filters, specifically <see cref="OnResourceExecutedAsync(ResourceExecutedContext)"/> calls.</param>
    public async Task OnResourceExecutedAsync(ResourceExecutedContext context) {
        if (_cacheResourceFilterOptions.DisableCache) {
            return;
        }
        var request = context.HttpContext.Request;
        var requestMethod = request.Method;
        var basePath = string.Empty;
        if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor) {
            var isCacheable = !actionDescriptor.MethodInfo.GetCustomAttributes(inherit: false).Any(x => x.GetType() == typeof(NoCacheAttribute));
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
            if (requestMethod == HttpMethod.Get.Method || requestMethod == HttpMethod.Head.Method) {
                var cachedValue = _cacheResourceKeysManager.GetString(_cacheKey);
                // Check if we already have a cached value for this cache key and also that response status code is 200 OK.
                if (string.IsNullOrEmpty(cachedValue) && (context.Result is OkObjectResult result)) {
                    _cacheResourceKeysManager.SetString(_cacheKey, JsonSerializer.Serialize(result.Value, _jsonSerializerOptions), new DistributedCacheEntryOptions {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expiration)
                    });
                }
            }
            if (requestMethod == HttpMethod.Post.Method || requestMethod == HttpMethod.Put.Method || requestMethod == HttpMethod.Patch.Method || requestMethod == HttpMethod.Delete.Method) {
                _cacheResourceKeysManager.Remove(_cacheKey);
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
                    _cacheResourceKeysManager.Remove(await AddCacheKeyDiscriminatorAsync(context.HttpContext, dependentKey));
                }
                foreach (var path in _dependentStaticPaths) {
                    var dependentKey = path.StartsWith('/') ? path : $"/{path}";
                    _cacheResourceKeysManager.Remove(await AddCacheKeyDiscriminatorAsync(context.HttpContext, dependentKey));
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next) {
        await OnResourceExecutingAsync(context);
        if (context.Result is null) {
            var executedContext = await next();
            await OnResourceExecutedAsync(executedContext);
        }
    }

    private async Task<string> AddCacheKeyDiscriminatorAsync(HttpContext httpContext, string keyMainPart) {
        if (_varyByClaimType.Length > 0) {
            var claimValues = _varyByClaimType.Select(claim => $"{claim}:{httpContext.User.FindFirstValue(claim)}");
            if (claimValues.Any()) {
                keyMainPart = $"{keyMainPart}|{string.Join('|', claimValues)}";
            }
        }
        if (_keyExtensionResolver is not null) {
            var keyExtension = await _keyExtensionResolver.ResolveCacheKeyExtensionAsync(httpContext, keyMainPart);
            if (keyExtension is not null) {
                keyMainPart = $"{keyMainPart}|{keyExtension}";
            }
        }
        return keyMainPart;
    }
}

/// <summary>An attribute that is used to indicate that a controller or method should not cache the response.</summary>
/// <remarks>Do not use this attribute together with <see cref="CacheResourceFilterAttribute"/> since the latter will have no effect.</remarks>
[AttributeUsage(validOn: AttributeTargets.Method)]
public sealed class NoCacheAttribute : Attribute { }

/// <summary>A type filter attribute for <see cref="CacheResourceFilter"/>.</summary>
/// <remarks>https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters#typefilterattribute</remarks>
[AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheResourceFilterAttribute : TypeFilterAttribute
{
    /// <summary>Creates a new instance of <see cref="CacheResourceFilterAttribute"/>.</summary>
    public CacheResourceFilterAttribute() : base(typeof(CacheResourceFilter)) {
        Arguments = new object[4];
        DependentPaths = [];
        DependentStaticPaths = [];
        Expiration = 60;
        VaryByClaimType = [];
    }

    /// <summary>Parent paths of the current method that must be invalidated. Path template variables must match by name.</summary>
    public string[] DependentPaths {
        get => (string[])Arguments[0];
        set => Arguments[0] = value;
    }
    /// <summary>Dependent static path that must be invalidated along with this resource.</summary>
    public string[] DependentStaticPaths {
        get => (string[])Arguments[1];
        set => Arguments[1] = value;
    }
    /// <summary>The absolute expiration in minutes of the cache item, expressed as an <see cref="int"/>. Defaults to 60 minutes.</summary>
    public int Expiration {
        get => (int)(Arguments[2] ?? 0);
        set => Arguments[2] = value;
    }
    /// <summary>The claim to use which value is included in the cache key.</summary>
    public string[] VaryByClaimType {
        get => (string[])Arguments[3];
        set => Arguments[3] = value;
    }
}
