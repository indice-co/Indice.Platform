using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Extensions
{

    /// <summary>
    /// ApplicationBuilder Extensions
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Setup the identityserver store configuration and operations
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder IdentityServerStoreSetup(this IApplicationBuilder app) {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                serviceScope.ServiceProvider.GetService<ConfigurationDbContext>().Database.EnsureCreated();
                serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.EnsureCreated();
                //EnsureSeedData(serviceScope.ServiceProvider.GetService<ConfigurationDbContext>());
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
            IEnumerable<IdentityServer4.Models.Client> clients, 
            IEnumerable<IdentityServer4.Models.IdentityResource> identityResources, 
            IEnumerable<IdentityServer4.Models.ApiResource> apis) {
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
