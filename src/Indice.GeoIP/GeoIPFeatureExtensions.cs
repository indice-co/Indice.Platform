using Indice.GeoIP.GeoLite2;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.GeoIP;

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
        services.AddSingleton<CityDatabaseReader>();
        services.AddSingleton<CountryDatabaseReader>();
        services.AddScoped<IPAddressLocator>();
        return services;
    }
}
