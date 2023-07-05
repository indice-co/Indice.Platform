using FluentValidation;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Server;
using Indice.Features.Risk.Server.Models;
using Indice.Features.Risk.Server.Validators;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/>.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
    /// <typeparam name="TRiskRequest">The type of risk request model.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Options for configuring the API for risk engine.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddRiskEndpoints<TRiskEvent, TRiskRequest>(this IServiceCollection services, Action<RiskApiOptions>? configureAction = null)
        where TRiskEvent : DbRiskEvent, new()
        where TRiskRequest : RiskRequestBase<TRiskEvent> {
        var riskApiOptions = new RiskApiOptions { 
            Services = services
        };
        configureAction?.Invoke(riskApiOptions);
        services.Configure<RiskApiOptions>(options => {
            options.ApiPrefix = riskApiOptions.ApiPrefix;
            options.ApiScope = riskApiOptions.ApiScope;
            options.AuthenticationScheme = riskApiOptions.AuthenticationScheme;
        });
        services.AddEndpointParameterFluentValidation(typeof(RiskApi).Assembly);
        services.AddTransient(typeof(IValidator<TRiskRequest>), typeof(GetRiskRequestValidator<TRiskEvent, TRiskRequest>));
        return services;
    }

    /// <summary>Registers the endpoints for the risk engine.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Options for configuring the API for risk engine.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddRiskEndpoints(this IServiceCollection services, Action<RiskApiOptions>? configureAction = null) =>
        services.AddRiskEndpoints<DbRiskEvent, RiskRequestBase<DbRiskEvent>>(configureAction);
}
