using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Indice.AspNetCore.Identity.Extensions;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Indice.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for configuring IdentityServer instance.
    /// </summary>
    public static class IdentityServerConfig
    {
        /// <summary>
        /// Configures the IdentityServer instance.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
        public static IIdentityServerBuilder AddIdentityServerConfig(this IServiceCollection services, IWebHostEnvironment hostingEnvironment, IConfiguration configuration, GeneralSettings settings) {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var identityServerBuilder = services.AddIdentityServer(options => {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseSuccessEvents = true;
                options.IssuerUri = settings.Host;
                options.UserInteraction.ErrorIdParameter = "errorId";
                options.UserInteraction.ErrorUrl = "/error";
                options.UserInteraction.LoginUrl = "/login";
                options.UserInteraction.LogoutUrl = "/logout";
            })
            .AddAspNetIdentity<User>()
            .AddOperationalStore(options => {
                options.SetupTables();
                options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("OperationalDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
                options.EnableTokenCleanup = true;
            })
            .AddConfigurationStore<ExtendedConfigurationDbContext>(options => {
                options.SetupTables();
                options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("ConfigurationDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
            })
            .AddTotp()
            .AddPushNotifications(
                 options => {
                     options.ConnectionString = configuration.GetConnectionString("PushNotificationsConnection");
                     options.NotificationHubPath = configuration["PushNotifications:PushNotificationsHubPath"];
                 }
             )
            .AddAppAuthRedirectUriValidator();
            if (hostingEnvironment.IsDevelopment()) {
                IdentityModelEventSource.ShowPII = true;
                identityServerBuilder.AddDeveloperSigningCredential();
            } else {
                var certificate = new X509Certificate2(Path.Combine(hostingEnvironment.ContentRootPath, "indice-idsrv.pfx"), configuration["IdentityServer:SigningPfxPass"], X509KeyStorageFlags.MachineKeySet);
                identityServerBuilder.AddSigningCredential(certificate);
            }
            return identityServerBuilder;
        }
    }
}
