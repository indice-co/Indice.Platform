using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine, using the provided type <typeparamref name="TTransaction"/>.</summary>
    /// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RiskEngineRuleBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static RiskEngineRuleBuilder<TTransaction> AddRiskEngine<TTransaction>(this IServiceCollection services) where TTransaction : TransactionBase {
        var builder = new RiskEngineRuleBuilder<TTransaction>(services);
        // Add core services.
        services.AddTransient<IRuleExecutionService<TTransaction>, RuleExecutionService<TTransaction>>();
        return builder;
    }

    /// <summary>Adds the required services for configuring the risk engine, using the default type <see cref="TransactionBase"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RiskEngineRuleBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static RiskEngineRuleBuilder<TransactionBase> AddRiskEngine(this IServiceCollection services) =>
        services.AddRiskEngine<TransactionBase>();
}
