using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Configuration;
using Indice.Identity;
using Microsoft.AspNetCore.Builder;
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
                // https://leastprivilege.com/2020/06/15/the-jwt-profile-for-oauth-2-0-access-tokens-and-identityserver/
                options.EmitScopesAsSpaceDelimitedStringInJwt = true;
            })
            .AddAspNetIdentity<User>()
            .AddTrustedDeviceAuthorization(options => {
                options.AddUserDeviceStoreEntityFrameworkCore();
            })
            //.AddPushNotifications()
            .AddExtendedResourceOwnerPasswordValidator()
            .AddOperationalStore(options => {
                options.SetupTables();
                options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("OperationalDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
                options.EnableTokenCleanup = true;
                options.TokenCleanupInterval = (int)TimeSpan.FromHours(1).TotalSeconds;
                options.TokenCleanupBatchSize = 100;
            })
            .AddConfigurationStore<ExtendedConfigurationDbContext>(options => {
                options.SetupTables();
                options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("ConfigurationDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
            })
            .AddTotp()
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
