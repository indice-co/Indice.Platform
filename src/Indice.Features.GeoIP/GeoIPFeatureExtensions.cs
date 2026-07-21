using Indice.Features.GeoIP;
using Indice.Features.GeoIP.GeoLite2;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add geolocation services.
/// </summary>
public static class GeoIPFeatureExtensions
{
    /// <summary>
    /// Adds the geolocation services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns></returns>
    public static IServiceCollection AddGeoIPResolver(this IServiceCollection services) {
        services.TryAddSingleton<CityDatabaseReader>();
        services.TryAddSingleton<CountryDatabaseReader>();
        services.TryAddSingleton<AsnDatabaseReader>();
        services.TryAddScoped<IPAddressLocator>();
        return services;
    }
}
