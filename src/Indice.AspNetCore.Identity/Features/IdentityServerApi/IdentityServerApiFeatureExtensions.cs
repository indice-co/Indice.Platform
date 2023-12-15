using System;
using Indice.AspNetCore.Identity.Api;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Api.Data;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.Configuration;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains extension methods on <see cref="IMvcBuilder"/> for configuring IdentityServer API feature.</summary>
public static class IdentityServerApiFeatureExtensions
{
    /// <summary>Adds the IdentityServer API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration options for IdentityServer API feature.</param>
    [Obsolete("This will be removed in a feature version. Please use MapIdentityServer extension method on type Microsoft.AspNetCore.Builder.WebApplication in order to use the Minimal API equivalent.")]
    public static IMvcBuilder AddIdentityServerApiEndpoints(this IMvcBuilder mvcBuilder, Action<IdentityServerApiEndpointsOptions> configureAction = null) {
        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new IdentityServerApiFeatureProvider()));
        var services = mvcBuilder.Services;
        var apiEndpointsOptions = new IdentityServerApiEndpointsOptions {
            Services = services
        };
        // Initialize default options.
        configureAction?.Invoke(apiEndpointsOptions);
        apiEndpointsOptions.Services = null;
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var dbContextOptions = serviceProvider.GetRequiredService<IdentityDbContextOptions>();
        services.AddDistributedMemoryCache();
        services.AddFeatureManagement(configuration.GetSection("IdentityServer:Features"));
        // Configure options for CacheResourceFilter.
        services.Configure<CacheResourceFilterOptions>(options => options.DisableCache = apiEndpointsOptions.DisableCache);
        // Invoke action provided by developer to override default options.
        services.AddSingleton(apiEndpointsOptions);
        services.AddGeneralSettings(configuration);
        services.AddDefaultPlatformEventService();
        services.TryAddScoped<IClientThemingService<DefaultClientThemeConfig>, ClientThemingService<DefaultClientThemeConfig>>();
        // Register validation filters.
        services.AddScoped<CreateClaimTypeRequestValidationFilter>();
        services.AddScoped<CreateRoleRequestValidationFilter>();
        // Add authorization policies that are used by the IdentityServer API.
        services.AddIdentityApiAuthorization();
        services.TryAddScoped<IdentityMessageDescriber>();
        // Try register the extended version of UserManager<User>.
        services.TryAddScoped<ExtendedUserManager<User>>();
        // Register the authentication handler, using a custom scheme name, for local APIs.
        services.AddAuthentication()
                .AddLocalApi(IdentityServerApi.AuthenticationScheme, options => {
                    options.ExpectedScope = IdentityServerApi.Scope;
                });
        return mvcBuilder;
    }

    /// <summary>Registers the <see cref="DbContext"/> to be used by the Identity system.</summary>
    /// <param name="options">Options for configuring the IdentityServer API feature.</param>
    /// <param name="configureAction">Configuration for <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</param>
    public static void AddDbContext(this IdentityServerApiEndpointsOptions options, Action<IdentityDbContextOptions> configureAction) {
        var dbContextOptions = new IdentityDbContextOptions();
        configureAction?.Invoke(dbContextOptions);
        options.Services.AddSingleton(dbContextOptions);
        options.Services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(dbContextOptions.ConfigureDbContext);
    }

    private static IServiceCollection AddIdentityApiAuthorization(this IServiceCollection services) => services.AddAuthorization(authOptions => {
        authOptions.AddPolicy(IdentityServerApi.Scope, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.Scope) || x.User.IsAdmin() || x.User.IsSystemClient());
        });
        authOptions.AddPolicy(IdentityServerApi.Policies.BeUsersReader, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.SubScopes.Users) && x.User.CanReadUsers());
        });
        authOptions.AddPolicy(IdentityServerApi.Policies.BeUsersWriter, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.SubScopes.Users) && x.User.CanWriteUsers());
        });
        authOptions.AddPolicy(IdentityServerApi.Policies.BeClientsReader, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.SubScopes.Clients) && x.User.CanReadClients());
        });
        authOptions.AddPolicy(IdentityServerApi.Policies.BeClientsWriter, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.SubScopes.Clients) && x.User.CanWriteClients());
        });
        authOptions.AddPolicy(IdentityServerApi.Policies.BeUsersOrClientsReader, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.SubScopes.Users) && (x.User.CanReadUsers() || x.User.CanReadClients()));
        });
        authOptions.AddPolicy(IdentityServerApi.Policies.BeAdmin, policy => {
            policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(x => x.User.HasScope(IdentityServerApi.Scope) && (x.User.HasRoleClaim(BasicRoleNames.Administrator) || x.User.HasRoleClaim(BasicRoleNames.AdminUIAdministrator) || x.User.IsAdmin() || x.User.IsSystemClient()));
        });
    });
}
