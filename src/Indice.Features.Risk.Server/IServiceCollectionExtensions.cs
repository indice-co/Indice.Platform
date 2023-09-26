using FluentValidation;
using Indice.Features.Risk.Server;
using Indice.Features.Risk.Server.Models;
using Indice.Features.Risk.Server.Validators;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/>.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Registers the endpoints for the risk engine.</summary>
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
            options.AuthenticationScheme = riskApiOptions.AuthenticationScheme;
        });
        services.AddEndpointParameterFluentValidation(typeof(RiskApi).Assembly);
        services.AddTransient<IValidator<RiskModel>, GetRiskRequestValidator>();
        return services;
    }
}
