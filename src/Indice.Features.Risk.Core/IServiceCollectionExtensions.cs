using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine, using the provided type <typeparamref name="TTransaction"/>.</summary>
    /// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configAction">Options for configuring the <b>Risk Engine</b> feature.</param>
    /// <returns>The <see cref="StoreBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder<TTransaction> AddRiskEngine<TTransaction>(this IServiceCollection services, Action<RiskEngineOptions>? configAction = null) where TTransaction : Transaction {
        var builder = new StoreBuilder<TTransaction>(services);
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
        services.AddTransient<RiskManager<TTransaction>>();
        return builder;
    }

    /// <summary>Adds the required services for configuring the risk engine, using the default type <see cref="Transaction"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configAction">Options for configuring the <b>Risk Engine</b> feature.</param>
    /// <returns>The <see cref="RuleBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder<Transaction> AddRiskEngine(this IServiceCollection services, Action<RiskEngineOptions>? configAction = null) =>
        services.AddRiskEngine<Transaction>(configAction);
}
