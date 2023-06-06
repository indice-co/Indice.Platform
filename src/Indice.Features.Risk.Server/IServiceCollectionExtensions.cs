using Indice.Features.Risk.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/>.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Registers the endpoints for </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Options for configuring the API for risk engine.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddRiskEndpoints(this IServiceCollection services, Action<RiskApiOptions>? configureAction = null) {
        var riskApiOptions = new RiskApiOptions {
            Services = services
        };
        configureAction?.Invoke(riskApiOptions);
        services.Configure<RiskApiOptions>(options => {
            options.ApiPrefix = riskApiOptions.ApiPrefix;
            options.ApiScope = riskApiOptions.ApiScope;
        });
        return services;
    }
}
