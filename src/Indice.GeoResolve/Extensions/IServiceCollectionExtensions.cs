using Indice.GeoResolve.GeoLite2;
using Indice.GeoResolve.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add geolocation services.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds the geolocation services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns></returns>
    public static IServiceCollection AddGeoResolver(this IServiceCollection services) {
        services.AddSingleton<CityDatabaseReader>();
        services.AddSingleton<CountryDatabaseReader>();
        services.AddScoped<IPAddressLocator>();
        return services;
    }
}
