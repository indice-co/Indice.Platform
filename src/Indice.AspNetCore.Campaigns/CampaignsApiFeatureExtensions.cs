using System;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods on <see cref="IMvcBuilder"/> for configuring Campaigns API feature.
    /// </summary>
    public static class CampaignsApiFeatureExtensions
    {
        /// <summary>
        /// Add the Campaigns API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
        public static IMvcBuilder AddCampaignsApiEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignsApiOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CampaignsApiFeatureProvider()));
            var services = mvcBuilder.Services;
            // Build service provider and get IConfiguration instance.
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            // Try add general settings.
            services.AddGeneralSettings(configuration);
            // Configure options given by the consumer.
            var featureOptions = new CampaignsApiOptions();
            configureAction?.Invoke(featureOptions);
            services.Configure<CampaignsApiOptions>(options => options = featureOptions);
            // Register other services.
            services.AddTransient<ICampaignService, CampaignService>();
            // Register application DbContext.
            if (featureOptions.ConfigureDbContext != null) {
                services.AddDbContext<CampaingsDbContext>(featureOptions.ConfigureDbContext);
            } else {
                services.AddDbContext<CampaingsDbContext>((builder) => builder.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            }
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(CampaignsApi.Policies.BeCampaignsManager, policy => {
                    policy.AddAuthenticationSchemes(CampaignsApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasScopeClaim(featureOptions.ExpectedScope ?? CampaignsApi.Scope) && x.User.CanManageCampaigns());
                });
            });
            return mvcBuilder;
        }
    }
}
