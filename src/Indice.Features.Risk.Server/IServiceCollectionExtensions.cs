using FluentValidation;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Server;
using Indice.Features.Risk.Server.Models;
using Indice.Features.Risk.Server.Validators;

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
    ) where TTransaction : Transaction {
        var riskApiOptions = new RiskApiOptions {
            Services = services,
            TransactionType = typeof(TTransaction)
        };
        configureAction?.Invoke(riskApiOptions);
        services.Configure<RiskApiOptions>(options => {
            options.ApiPrefix = riskApiOptions.ApiPrefix;
            options.ApiScope = riskApiOptions.ApiScope;
            options.AuthenticationScheme = riskApiOptions.AuthenticationScheme;
        });
        services.AddEndpointParameterFluentValidation();
        services.AddScoped<IValidator<CreateTransactionEventRequest>, CreateTransactionEventCommandValidator<TTransaction>>();
        return services;
    }

    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Options for configuring the API for risk engine.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddRiskEndpoints(
        this IServiceCollection services,
        Action<RiskApiOptions>? configureAction = null
    ) => services.AddRiskEndpoints<Transaction>(configureAction);
}
