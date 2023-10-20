using Indice.Features.GovGr;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Proxies.Gsis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Configuration extensions regarding eKYC for gov GR</summary>
public static class GovGrKycConfigurationExtensions
{
    /// <summary>Adds GovGr Http services (kyc wallet etc.).</summary>
    public static IServiceCollection AddGovGrClient(this IServiceCollection services, Action<GovGrOptions> configure = null) {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Initialize empty options.
        var options = new GovGrOptions();
        // Bind 'EGovKycSettings' section from application settings to 'options' object.
        configuration.Bind(GovGrOptions.Name, options);
        // Invoke provided action from caller and override 'options' object.
        configure?.Invoke(options);
        // Register options in the DI container.
        services.Configure<GovGrOptions>(settings => {
            settings.UseMockServices = options.UseMockServices;
            settings.Kyc = options.Kyc;
            settings.Wallet = options.Wallet;
            settings.Documents = options.Documents;
            settings.BusinessRegistry = options.BusinessRegistry;
        });
        // Register custom services.
        services.AddLocalization();
        services.AddHttpClient();
        services.AddTransient<GovGrClient>();
        services.AddTransient<RgWsPublic, RgWsPublicClient>(sp => {
            var client = new RgWsPublicClient(options.BusinessRegistry);
            return client;
        });
        services.AddTransient(sp => new Func<RgWsPublic>(sp.GetRequiredService<RgWsPublic>));
        services.AddTransient<GovGrKycScopeDescriber>();
        return services;
    }
}
