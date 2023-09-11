using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configAction">Options for configuring the <b>Risk Engine</b> feature.</param>
    /// <returns>The <see cref="StoreBuilder"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder AddRiskEngine(this IServiceCollection services, Action<RiskEngineOptions>? configAction = null) {
        var builder = new StoreBuilder(services);
        var options = new RiskEngineOptions();
        configAction?.Invoke(options);
        var result = options.Validate();
        if (!result.Succeeded) {
            throw new InvalidOperationException($"Options of type {nameof(RiskEngineOptions)} have the following error: {result.ErrorMessage}");
        }
        services.Configure<RiskEngineOptions>(riskOptions => {
            riskOptions.RiskLevelRangeMapping = options.RiskLevelRangeMapping;
        });
        services.AddTransient<RiskManager>();
        return builder;
    }
}
