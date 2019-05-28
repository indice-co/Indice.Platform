using System.Collections.Generic;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on type <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Setup the IdentityServer stores for clients as well as API and identity resources.
        /// </summary>
        /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="clients">The list of predefined in-memory clients.</param>
        /// <param name="identityResources">The list of predefined in-memory identity resources.</param>
        /// <param name="apiResources">The list of predefined in-memory API resources.</param>
        public static IApplicationBuilder IdentityServerStoreSetup(this IApplicationBuilder app, IEnumerable<Client> clients = null, IEnumerable<IdentityResource> identityResources = null, IEnumerable<ApiResource> apiResources = null) {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.EnsureCreated();
                var config = serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                if (config != null) {
                    config.Database.EnsureCreated();
                    config.SeedData(clients, identityResources, apiResources);
                }
            }
            return app;
        }

        /// <summary>
        /// Helper that seeds data to the <see cref="ConfigurationDbContext"/> store using in-memory configuration as the initial load.
        /// Works only if no data exist in the database.
        /// </summary>
        /// <param name="context">DbContext for the IdentityServer configuration data.</param>
        /// <param name="clients">The list of predefined in-memory clients.</param>
        /// <param name="identityResources">The list of predefined in-memory identity resources.</param>
        /// <param name="apiResources">The list of predefined in-memory API resources.</param>
        public static void SeedData(this ConfigurationDbContext context, IEnumerable<Client> clients, IEnumerable<IdentityResource> identityResources, IEnumerable<ApiResource> apiResources) {
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
            if (!context.ApiResources.Any() && apiResources != null) {
                foreach (var resource in apiResources) {
                    context.ApiResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }
        }
    }
}
