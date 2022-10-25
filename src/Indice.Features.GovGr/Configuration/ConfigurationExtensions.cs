using System;
using Indice.Features.GovGr;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configuration extensions regarding eKYC for gov GR
    /// </summary>
    public static class GovGrKycConfigurationExtensions
    {
        /// <summary>
        /// Adds GovGr Http services (kyc wallet etc.).
        /// </summary>
        public static IServiceCollection AddGovGrClient(this IServiceCollection services, Action<GovGrSettings> configure = null) {
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            // Initialize empty options.
            var options = new GovGrSettings();
            // Bind 'EGovKycSettings' section from application settings to 'options' object.
            configuration.Bind(GovGrSettings.Name, options);
            // Invoke provided action from caller and override 'options' object.
            configure?.Invoke(options);
            // Register options in the DI container.
            services.Configure<GovGrSettings>(settings => {
                settings.TokenEndpoint = options.TokenEndpoint;
                settings.ResourceServerEndpoint = options.ResourceServerEndpoint;
                settings.UseMockServices = options.UseMockServices;
                settings.Clients = options.Clients;
                settings.SkipCheckTin = options.SkipCheckTin;
            });
            // Register custom services.
            services.AddLocalization();
            services.AddHttpClient();
            services.AddTransient<GovGrClient>();
            services.AddTransient<GovGrKycScopeDescriber>();
            return services;
        }
    }
}
