using System.ComponentModel.DataAnnotations;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Core.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Extensions;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configAction">Options for configuring the <b>Risk Engine</b> feature.</param>
    /// <returns>The <see cref="RiskEngineBuilder"/> instance used to configure the risk engine rules.</returns>
    public static RiskEngineBuilder AddRiskEngine(this IServiceCollection services, Action<RiskEngineOptions>? configAction = null) {
        var builder = new RiskEngineBuilder(services);
        var options = new RiskEngineOptions();
        configAction?.Invoke(options);
        var result = options.Validate();
        if (result != ValidationResult.Success) {
            throw new InvalidOperationException($"Options of type {nameof(RiskEngineOptions)} have the following error: {result?.ErrorMessage}");
        }
        services.Configure<RiskEngineOptions>(riskOptions => {
            riskOptions.RiskLevelRangeMapping = options.RiskLevelRangeMapping;
        });
        services.AddTransient<RiskManager>();
        services.AddSingleton<IRiskEventStore, RiskEventStoreNoOp>();
        return builder;
    }
}
