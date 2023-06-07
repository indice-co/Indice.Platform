using Indice.Features.Risk.Core;
using Indice.Features.Risk.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/>.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <typeparam name="TTransaction">The model type of the transaction.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Options for configuring the API for risk engine.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddRiskEndpoints<TTransaction>(
        this IServiceCollection services,
        Action<RiskApiOptions>? configureAction = null
    ) where TTransaction : TransactionBase {
        var riskApiOptions = new RiskApiOptions {
            Services = services,
            TransactionType = typeof(TTransaction)
        };
        configureAction?.Invoke(riskApiOptions);
        services.Configure<RiskApiOptions>(options => {
            options.ApiPrefix = riskApiOptions.ApiPrefix;
            options.ApiScope = riskApiOptions.ApiScope;
        });
        return services;
    }

    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Options for configuring the API for risk engine.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddRiskEndpoints(
        this IServiceCollection services,
        Action<RiskApiOptions>? configureAction = null
    ) => services.AddRiskEndpoints<TransactionBase>(configureAction);
}
