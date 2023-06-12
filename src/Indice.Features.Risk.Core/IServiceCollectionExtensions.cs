using Indice.Features.Risk.Core.Abstractions;
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
    /// <returns>The <see cref="StoreBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder<TTransaction> AddRiskEngine<TTransaction>(this IServiceCollection services) where TTransaction : Transaction {
        var builder = new StoreBuilder<TTransaction>(services);
        // Add core services.
        services.AddTransient<IRuleExecutionService<TTransaction>, RuleExecutionService<TTransaction>>();
        return builder;
    }

    /// <summary>Adds the required services for configuring the risk engine, using the default type <see cref="Transaction"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RuleBuilder{TTransaction}"/> instance used to configure the risk engine.</returns>
    public static StoreBuilder<Transaction> AddRiskEngine(this IServiceCollection services) =>
        services.AddRiskEngine<Transaction>();
}
