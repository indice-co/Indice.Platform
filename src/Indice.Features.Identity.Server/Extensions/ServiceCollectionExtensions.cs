using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using Indice.Configuration;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Options;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions on the IServiceCollection
/// </summary>
public static class IdentityServerEndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds Scalefin Server.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns>The <see cref="IExtendedIdentityServerBuilder"/></returns>
    public static IExtendedIdentityServerBuilder AddExtendedIdentityServer(this IServiceCollection services, Action<ExtendedIdentityServerOptions> setupAction = null) {
        var extendedOptions = new ExtendedIdentityServerOptions();
        setupAction?.Invoke(extendedOptions);
        services.AddSingleton(extendedOptions);
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        services.Configure<IdentityOptions>(configuration.GetSection(nameof(IdentityOptions)));
        services.Configure<CookieTempDataProviderOptions>(options => options.Cookie.Expiration = TimeSpan.FromMinutes(30));
        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorRememberMeScheme, options => {
            options.ExpireTimeSpan = TimeSpan.FromDays(configuration.GetIdentityOption<int?>("SignIn:Mfa", "RememberDurationInDays") ?? ExtendedSignInManager<User>.DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS);
        });
        var innerServerBuilder = services.AddIdentityServer(options => {
            options.IssuerUri = configuration.GetHost();
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.UserInteraction.LoginUrl = "/login";
            options.UserInteraction.LogoutUrl = "/logout";
            options.UserInteraction.ErrorUrl = "/error";
            options.UserInteraction.ErrorIdParameter = "errorId";
            options.EmitScopesAsSpaceDelimitedStringInJwt = true;
        });
        var innerIdentityBuilder = services.AddIdentityDefaults();
        var builder = new ExtendedIdentityServerBuilder(serviceProvider, innerServerBuilder, innerIdentityBuilder, configuration, environment);

        //builder.Services.AddTransient<IAccountService, AccountService>();

        builder.AddConfigurationStore<ExtendedConfigurationDbContext>(options => {
            options.SetupTables();
            options.ConfigureDbContext = dbBuilder => dbBuilder.UseSqlServer(extendedOptions.ConfigurationDbConnectionString ?? builder.Configuration.GetConnectionString("ConfigurationDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(typeof(ExtendedConfigurationDbContext).Assembly.GetName().Name));
        })
        .AddOperationalStore(options => {
            options.SetupTables();
            options.EnableTokenCleanup = true;
            options.TokenCleanupInterval = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.TokenCleanupBatchSize = 250;
            options.ConfigureDbContext = dbBuilder => dbBuilder.UseSqlServer(extendedOptions.OperationalDbConnectionString ?? builder.Configuration.GetConnectionString("OperationalDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(typeof(ExtendedConfigurationDbContext).Assembly.GetName().Name));
        })
        .AddDelegationGrantValidator()
        .AddJwtBearerClientAuthentication()
        .AddAspNetIdentity<User>()
        .AddAppAuthRedirectUriValidator();
        if (builder.Environment.IsDevelopment()) {
            IdentityModelEventSource.ShowPII = true;
            builder.AddDeveloperSigningCredential();
        } else {
            var certificate = new X509Certificate2(Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["IdentityServer:SigningPfxFile"]), builder.Configuration["IdentityServer:SigningPfxPass"], X509KeyStorageFlags.MachineKeySet);
            builder.AddSigningCredential(certificate);
        }
        builder.AddDotnet7CompatibleStores();
        return builder;
    }

    /// <summary>
    /// Creates databases
    /// </summary>
    /// <param name="extendedIdentityServerBuilder"></param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder CreateDataStores(this IExtendedIdentityServerBuilder extendedIdentityServerBuilder) {
        var serviceProvider = extendedIdentityServerBuilder.Services.BuildServiceProvider();
        serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>().Database.EnsureCreated();
        return extendedIdentityServerBuilder;
    }

    private static IdentityBuilder AddIdentityDefaults(this IServiceCollection services) {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        services.AddExternalProviderClaimsTransformation();
        services.TryAddScoped<IdentityMessageDescriber>();
        services.TryAddScoped<ExtendedUserManager<User>>(); // Try register the extended version of UserManager<User>.
        return services.AddIdentity<User, Role>()
                              .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                              .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                              .AddExtendedSignInManager<User>()
                              .AddDefaultPasswordValidators()
                              .AddClaimsPrincipalFactory<ExtendedUserClaimsPrincipalFactory<User, Role>>()
                              .AddDefaultTokenProviders()
                              .AddExtendedPhoneNumberTokenProvider();
    }

    /// <summary>
    /// Adds Identity server management endpoints dependencies. 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddExtendedEndpoints(this IExtendedIdentityServerBuilder builder) {
        var builderOptions = builder.ServiceProvider.GetRequiredService<ExtendedIdentityServerOptions>();
        // Configure anti-forgery token options.
        builder.Services.Configure<AntiforgeryOptions>(options => options.HeaderName = CustomHeaderNames.AntiforgeryHeaderName);
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddFeatureManagement(builder.Configuration.GetSection("IdentityServer:Features"));
        // Configure options for CacheResourceFilter.
        builder.Services.Configure<CacheResourceFilterOptions>(options => options.DisableCache = builderOptions.DisableCache);
        // Invoke action provided by developer to override default options.
        builder.Services.TryAddTransient<IPlatformEventService, PlatformEventService>();
        builder.Services.TryAddScoped<IClientThemingService<DefaultClientThemeConfig>, ClientThemingService<DefaultClientThemeConfig>>();
        // Add authorization policies that are used by the IdentityServer API.
        builder.Services.AddAuthorization(authOptions => {
            authOptions.AddPolicy(IdentityServerNames.Scope, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.Scope) || x.User.IsAdmin() || x.User.IsSystemClient());
            });
            authOptions.AddPolicy(IdentityServerNames.Policies.BeUsersReader, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.SubScopes.Users) && x.User.CanReadUsers());
            });
            authOptions.AddPolicy(IdentityServerNames.Policies.BeUsersWriter, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.SubScopes.Users) && x.User.CanWriteUsers());
            });
            authOptions.AddPolicy(IdentityServerNames.Policies.BeClientsReader, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.SubScopes.Clients) && x.User.CanReadClients());
            });
            authOptions.AddPolicy(IdentityServerNames.Policies.BeClientsWriter, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.SubScopes.Clients) && x.User.CanWriteClients());
            });
            authOptions.AddPolicy(IdentityServerNames.Policies.BeUsersOrClientsReader, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.SubScopes.Users) && (x.User.CanReadUsers() || x.User.CanReadClients()));
            });
            authOptions.AddPolicy(IdentityServerNames.Policies.BeAdmin, policy => {
                policy.AddAuthenticationSchemes(IdentityServerNames.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityServerNames.Scope) && (x.User.HasRoleClaim(BasicRoleNames.Administrator) || x.User.HasRoleClaim(BasicRoleNames.AdminUIAdministrator) || x.User.IsAdmin() || x.User.IsSystemClient()));
            });


        });
        // Register the authentication handler, using a custom scheme name, for local APIs.
        builder.Services.AddAuthentication()
                .AddLocalApi(IdentityServerNames.AuthenticationScheme, options => {
                    options.ExpectedScope = IdentityServerNames.Scope;
                });

        return builder;
    }

    /// <summary>Registers the <see cref="DbContext"/> to be used by the Identity system.</summary>
    /// <param name="options">Options for configuring the IdentityServer API feature.</param>
    /// <param name="configureAction">Configuration for <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</param>
    public static void AddExtendedDbContext(this IExtendedIdentityServerBuilder options, Action<IdentityDbContextOptions> configureAction) {
        var dbContextOptions = new IdentityDbContextOptions();
        configureAction?.Invoke(dbContextOptions);
        options.Services.AddSingleton(dbContextOptions);
        options.Services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(dbContextOptions.ConfigureDbContext);
    }
}
