using System;
using Indice.Features.GovGr;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.Kyc.GovGr;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configuration extensions regarding eKYC for gov GR
    /// </summary>
    public static class GovGrKycConfigurationExtensions
    {
        /// <summary>
        /// Add GovGr Kyc service.
        /// </summary>
        public static void AddKycGovGr(this IServiceCollection services, Action<GovGrKycSettings> configure = null) {
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            // Initialize empty options.
            var options = new GovGrKycSettings();
            // Bind 'EGovKycSettings' section from application settings to 'options' object.
            configuration.Bind(GovGrKycSettings.Name, options);
            // Invoke provided action from caller and override 'options' object.
            configure?.Invoke(options);
            // Register options in the DI container.
            services.Configure<GovGrKycSettings>(settings => {
                settings.TokenEndpoint = options.TokenEndpoint;
                settings.ResourceServerEndpoint = options.ResourceServerEndpoint;
                settings.UseMockServices = options.UseMockServices;
                settings.Clients = options.Clients;
                settings.SkipCheckTin = options.SkipCheckTin;
            });
            // Register custom services.
            services.AddTransient<GovGrKycScopeDescriber>();
            services.AddHttpClient<GovGrClient>();

        }
    }
}
