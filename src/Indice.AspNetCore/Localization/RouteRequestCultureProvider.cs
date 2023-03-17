using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Localization;

// You can take a look at the default <see cref="QueryStringRequestCultureProvider"/> implementation to learn more
// https://github.com/aspnet/Localization/blob/dev/src/Microsoft.AspNetCore.Localization/QueryStringRequestCultureProvider.cs
/// <summary>Determines the culture information for a request via the value of a predefined route parameter. Defaults to "culture" route key</summary>
public class RouteRequestCultureProvider : IRequestCultureProvider
{
    /// <summary>Result that indicates that this instance of <see cref="RouteRequestCultureProvider" /> could not determine the request culture.</summary>
    protected static readonly Task<ProviderCultureResult> NullProviderCultureResult = Task.FromResult(default(ProviderCultureResult));

    /// <summary>Route parameter key</summary>
    public string RouteParameterKey { get; set; } = "culture";

    /// <inheritdoc/>
    public Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext) {
        if (httpContext == null) {
            throw new ArgumentNullException(nameof(httpContext));
        }

        var culture = httpContext.GetRouteValue(RouteParameterKey);

        if (culture == null) {
            return NullProviderCultureResult;
        }

        var providerResultCulture = new ProviderCultureResult(culture.ToString());
        return Task.FromResult(providerResultCulture);
    }
}
