using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine, using the provided type <typeparamref name="TRiskEvent"/>.</summary>
    /// <typeparam name="TRiskEvent">The type of transaction that the engine manages.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configAction">Options for configuring the <b>Risk Engine</b> feature.</param>
    /// <returns>The <see cref="StoreBuilder{TRiskEvent}"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder<TRiskEvent> AddRiskEngine<TRiskEvent>(this IServiceCollection services, Action<RiskEngineOptions>? configAction = null) where TRiskEvent : DbRiskEvent {
        var builder = new StoreBuilder<TRiskEvent>(services);
        var options = new RiskEngineOptions();
        configAction?.Invoke(options);
        var result = options.Validate();
        if (!result.Succeeded) {
            throw new InvalidOperationException($"Options of type {nameof(RiskEngineOptions)} have the following error: {result.ErrorMessage}");
        }
        services.Configure<RiskEngineOptions>(riskOptions => {
            riskOptions.RiskLevelRangeMapping = options.RiskLevelRangeMapping;
        });
        // Add core services.
        services.AddTransient<RiskManager<TRiskEvent>>();
        return builder;
    }

    /// <summary>Adds the required services for configuring the risk engine, using the default type <see cref="DbRiskEvent"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configAction">Options for configuring the <b>Risk Engine</b> feature.</param>
    /// <returns>The <see cref="RuleBuilder{TRiskEvent}"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder<DbRiskEvent> AddRiskEngine(this IServiceCollection services, Action<RiskEngineOptions>? configAction = null) =>
        services.AddRiskEngine<DbRiskEvent>(configAction);
}
