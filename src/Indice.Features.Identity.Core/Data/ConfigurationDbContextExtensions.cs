using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

#if NET7_0_OR_GREATER
using Indice.IdentityServer.EntityFramework.Storage.Mappers;
#else
using IdentityServer4.EntityFramework.Mappers;
#endif

namespace Indice.Features.Identity.Core.Data;

/// <summary>Extensions on type <see cref="IApplicationBuilder"/>.</summary>
public static class ConfigurationExtensions
{
    /// <summary>Sets up the IdentityServer stores for clients as well as API and identity resources.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="clients">The list of predefined clients.</param>
    /// <param name="identityResources">The list of predefined identity resources.</param>
    /// <param name="apis">The list of predefined APIs.</param>
    /// <param name="apiScopes">The list of predefined API scopes.</param>
    public static IApplicationBuilder ConfigurationStoreSetup<TConfigurationDbContext>(this IApplicationBuilder app,
        IEnumerable<Client> clients = null,
        IEnumerable<IdentityResource> identityResources = null,
        IEnumerable<ApiResource> apis = null,
        IEnumerable<ApiScope> apiScopes = null) where TConfigurationDbContext : ConfigurationDbContext<TConfigurationDbContext> {
        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var dbContext = serviceScope.ServiceProvider.GetService<TConfigurationDbContext>();
            if (dbContext is not null && dbContext.Database.EnsureCreated()) {
                dbContext.SeedData(clients, identityResources, apis, apiScopes);
                if (dbContext is ExtendedConfigurationDbContext extendedDbContext) {
                    extendedDbContext.SeedInitialClaimTypes();
                }
            }
        }
        return app;
    }

    /// <summary>Sets up the IdentityServer stores for clients as well as API and identity resources.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="clients">The list of predefined clients.</param>
    /// <param name="identityResources">The list of predefined identity resources.</param>
    /// <param name="apis">The list of predefined API resources.</param>
    /// <param name="apiScopes">The list of predefined API scopes.</param>
    public static IApplicationBuilder ConfigurationStoreSetup(this IApplicationBuilder app, IEnumerable<Client> clients = null, IEnumerable<IdentityResource> identityResources = null, IEnumerable<ApiResource> apis = null,
        IEnumerable<ApiScope> apiScopes = null) =>
        app.ConfigurationStoreSetup<ConfigurationDbContext>(clients, identityResources, apis, apiScopes);

    /// <summary>Sets up the IdentityServer operational store.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    public static IApplicationBuilder OperationalStoreSetup(this IApplicationBuilder app) {
        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var dbContext = serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>();
            if (dbContext is not null) {
                dbContext.Database.EnsureCreated();
            }
        }
        return app;
    }

    /// <summary>
    /// Helper that seeds data to the <see cref="ConfigurationDbContext{TConfigurationDbContext}"/> store using configuration as the initial load.
    /// Works only if no data exist in the database.
    /// </summary>
    /// <param name="context">DbContext for the IdentityServer configuration data.</param>
    /// <param name="clients">The list of predefined clients.</param>
    /// <param name="identityResources">The list of predefined identity resources.</param>
    /// <param name="apis">The list of predefined API resources.</param>
    /// <param name="apiScopes">The list of predefined API scopes.</param>
    public static void SeedData<TConfigurationDbContext>(this TConfigurationDbContext context, IEnumerable<Client> clients, IEnumerable<IdentityResource> identityResources, IEnumerable<ApiResource> apis, IEnumerable<ApiScope> apiScopes)
        where TConfigurationDbContext : ConfigurationDbContext<TConfigurationDbContext> {
        if (!context.Clients.Any() && clients != null) {
            foreach (var client in clients) {
                var clientEntity = client.ToEntity();
                // Make initial system clients non-editable.
                clientEntity.NonEditable = true;
                context.Clients.Add(clientEntity);
            }
            context.SaveChanges();
        }
        if (!context.IdentityResources.Any() && identityResources != null) {
            foreach (var resource in identityResources) {
                var resourceEntity = resource.ToEntity();
                // Make initial system resources non-editable.
                resourceEntity.NonEditable = true;
                context.IdentityResources.Add(resourceEntity);
            }
            context.SaveChanges();
        }
        if (!context.ApiScopes.Any() && apiScopes != null) {
            foreach (var apiScope in apiScopes) {
                var apiScopeEntity = apiScope.ToEntity();
                context.ApiScopes.Add(apiScopeEntity);
            }
            context.SaveChanges();
        }
        if (!context.ApiResources.Any() && apis != null) {
            foreach (var resource in apis) {
                var resourceEntity = resource.ToEntity();
                resourceEntity.NonEditable = true;
                context.ApiResources.Add(resourceEntity);
            }
            context.SaveChanges();
        }
    }

    /// <summary>Helper that seeds data to the <see cref="ConfigurationDbContext"/> store using configuration as the initial load. Works only if no data exist in the database.
    /// </summary>
    /// <param name="context">DbContext for the IdentityServer configuration data.</param>
    /// <param name="clients">The list of predefined clients.</param>
    /// <param name="identityResources">The list of predefined identity resources.</param>
    /// <param name="apis">The list of predefined API resources.</param>
    /// <param name="apiScopes">The list of predefined API scopes.</param>
    public static void SeedData(this ConfigurationDbContext context, IEnumerable<Client> clients, IEnumerable<IdentityResource> identityResources, IEnumerable<ApiResource> apis, IEnumerable<ApiScope> apiScopes) =>
        context.SeedData<ConfigurationDbContext>(clients, identityResources, apis, apiScopes);
}
