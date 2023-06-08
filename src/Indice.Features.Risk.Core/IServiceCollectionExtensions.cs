using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine, using the provided type <typeparamref name="TTransaction"/>.</summary>
    /// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RiskEngineStoreBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static RiskEngineStoreBuilder<TTransaction> AddRiskEngine<TTransaction>(this IServiceCollection services) where TTransaction : Transaction {
        var builder = new RiskEngineStoreBuilder<TTransaction>(services);
        // Add core services.
        services.AddTransient<IRuleExecutionService<TTransaction>, RuleExecutionService<TTransaction>>();
        return builder;
    }

    /// <summary>Adds the required services for configuring the risk engine, using the default type <see cref="Transaction"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RiskEngineRuleBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static RiskEngineStoreBuilder<Transaction> AddRiskEngine(this IServiceCollection services) =>
        services.AddRiskEngine<Transaction>();
}
