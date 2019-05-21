using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.Filters;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// ApplicationBuilder Extensions
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds content security policy. See also <see cref="SecurityHeadersAttribute"/> that enables the policy on a specific action
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddCsp(this IServiceCollection services, Action<CSP> configureAction = null) {
            services.ConfigureOptions<CSP>();
            if (configureAction != null)
                services.Configure(configureAction);
            return services;
        }


        /// <summary>
        /// Setup the identityserver store configuration and operations
        /// </summary>
        /// <param name="app"></param>
        /// <param name="clients"></param>
        /// <param name="identityResources"></param>
        /// <param name="apis"></param>
        /// <returns></returns>
        public static IApplicationBuilder IdentityServerStoreSetup(this IApplicationBuilder app,
            IEnumerable<Client> clients = null,
            IEnumerable<IdentityResource> identityResources = null,
            IEnumerable<ApiResource> apis = null) {

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.EnsureCreated();
                var config = serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                if (config != null) { 
                    config.Database.EnsureCreated();
                    config.SeedData(clients, identityResources, apis);
                }
            }

            return app;
        }

        /// <summary>
        /// Helper that seeds data to the <see cref="ConfigurationDbContext"/> store using in memory class configuration as the initial load.
        /// Works only if no data exist in the database.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clients"></param>
        /// <param name="identityResources"></param>
        /// <param name="apis"></param>
        public static void SeedData(this ConfigurationDbContext context,
            IEnumerable<Client> clients,
            IEnumerable<IdentityResource> identityResources,
            IEnumerable<ApiResource> apis) {

            if (!context.Clients.Any() && clients != null) {
                foreach (var client in clients) {
                    context.Clients.Add(client.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.IdentityResources.Any() && identityResources != null) {
                foreach (var resource in identityResources) {
                    context.IdentityResources.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.ApiResources.Any() && apis != null) {
                foreach (var resource in apis) {
                    context.ApiResources.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }
        }
    }
}
