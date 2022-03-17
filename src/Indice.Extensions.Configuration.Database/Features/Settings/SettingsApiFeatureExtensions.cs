using Indice.AspNetCore.Features.Settings;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods on <see cref="IMvcBuilder"/> for configuring Settings API feature.
    /// </summary>
    public static class SettingsApiFeatureExtensions
    {
        /// <summary>
        /// Add the Settings API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Settings API feature.</param>
        public static IMvcBuilder AddSettingsApiEndpoints(this IMvcBuilder mvcBuilder, Action<SettingsApiOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new SettingsApiFeatureProvider()));
            var services = mvcBuilder.Services;
            // Build service provider and get IConfiguration instance.
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            // Configure options given by the consumer.
            var settingsApiOptions = new SettingsApiOptions {
                Services = services
            };
            configureAction?.Invoke(settingsApiOptions);
            settingsApiOptions.Services = null;
            services.Configure<SettingsApiOptions>(options => {
                options.ApiPrefix = settingsApiOptions.ApiPrefix;
                options.ConfigureDbContext = settingsApiOptions.ConfigureDbContext;
            });
            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.Conventions.Add(new ApiPrefixControllerModelConvention("[settingsApiPrefix]", settingsApiOptions.ApiPrefix ?? "api"));
            });
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(SettingsApi.Policies.BeSettingsManager, policy => {
                    policy.AddAuthenticationSchemes("Bearer")
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => (string.IsNullOrWhiteSpace(settingsApiOptions.RequiredScope) ? x.User.HasScopeClaim(SettingsApi.Scope) : true) && x.User.IsAdmin());
                });
            });
            return mvcBuilder;
        }
    }
}
