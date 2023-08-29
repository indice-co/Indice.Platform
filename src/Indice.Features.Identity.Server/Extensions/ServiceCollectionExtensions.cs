using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using IdentityModel;
using Indice.Configuration;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Options;
using Indice.Features.Identity.Server.Totp.Models;
using Indice.Features.Identity.Server.Totp.Validators;
using Indice.Security;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on the <see cref="IServiceCollection"/> that help setup Indice Identity system.</summary>
public static class IdentityServerEndpointServiceCollectionExtensions
{
    /// <summary>Adds an extended version of IdentityServer.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="environment">Provides information about the web hosting environment an application is running in.</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns>The <see cref="IExtendedIdentityServerBuilder"/>.</returns>
    public static IExtendedIdentityServerBuilder AddExtendedIdentityServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        Action<ExtendedIdentityServerOptions>? setupAction = null
    ) {
        var options = new ExtendedIdentityServerOptions();
        setupAction?.Invoke(options);
        services.AddSingleton(options);
        services.TryAddTransient(serviceProvider => new ExtendedIdentityDbContextSeedOptions<User> {
            InitialUsers = serviceProvider.GetRequiredService<ExtendedIdentityServerOptions>().InitialUsers
        });
        services.AddTransient(serviceProvider => new ExtendedConfigurationDbContextSeedOptions {
            CustomClaims = serviceProvider.GetRequiredService<ExtendedIdentityServerOptions>().CustomClaims
        });
        services.AddTotpServiceFactory(configuration);
        var identityBuilder = services.AddIdentityDefaults(configuration);
        var identityServerBuilder = services.AddIdentityServerDefaults(configuration, environment, options.ConfigureConfigurationDbContext, options.ConfigurePersistedGrantDbContext);
        services.AddAuthenticationDefaults();
        options.ConfigureIdentityDbContext ??= dbBuilder => dbBuilder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
        services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(options.ConfigureIdentityDbContext);
        services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromMinutes(10);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        return new ExtendedIdentityServerBuilder(identityServerBuilder, identityBuilder, configuration, environment);
    }

    private static IdentityBuilder AddIdentityDefaults(
        this IServiceCollection services,
        IConfiguration configuration
    ) {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        services.Configure<IdentityOptions>(configuration.GetSection(nameof(IdentityOptions)));
        services.AddExternalProviderClaimsTransformation();
        services.Configure<CookieTempDataProviderOptions>(options => {
            options.Cookie.Expiration = TimeSpan.FromMinutes(30);
        });
        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorRememberMeScheme, options => {
            options.ExpireTimeSpan = TimeSpan.FromDays(configuration.GetIdentityOption<int?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "RememberDurationInDays") ?? ExtendedSignInManager<User>.DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS);
        });
        services.TryAddScoped<IdentityMessageDescriber>();
        return services.AddIdentity<User, Role>()
                       .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                       .AddExtendedUserManager()
                       .AddExtendedSignInManager<User>()
                       .AddDefaultPasswordValidators()
                       .AddClaimsPrincipalFactory<ExtendedUserClaimsPrincipalFactory<User, Role>>()
                       .AddDefaultTokenProviders()
                       .AddExtendedPhoneNumberTokenProvider(configuration)
                       .AddExtendedEmailTokenProvider(configuration);
    }

    private static IIdentityServerBuilder AddIdentityServerDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment,
        Action<DbContextOptionsBuilder>? configureConfigurationDbContext,
        Action<DbContextOptionsBuilder>? configurePersistedGrantDbContext
    ) {
        var identityServerBuilder = services.AddIdentityServer(options => {
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
        })
        .AddConfigurationStore<ExtendedConfigurationDbContext>(options => {
            options.SetupTables();
            options.ConfigureDbContext = configureConfigurationDbContext ??= dbBuilder => dbBuilder.UseSqlServer(configuration.GetConnectionString("ConfigurationDb"));
        })
        .AddOperationalStore(options => {
            options.SetupTables();
            options.EnableTokenCleanup = true;
            options.TokenCleanupInterval = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.TokenCleanupBatchSize = 250;
            options.ConfigureDbContext = configurePersistedGrantDbContext ??= dbBuilder => dbBuilder.UseSqlServer(configuration.GetConnectionString("OperationalDb"));
        })
        .AddJwtBearerClientAuthentication()
        .AddAspNetIdentity<User>()
        .AddAppAuthRedirectUriValidator()
        .AddOtpAuthenticateGrantValidator()
        .AddDelegationGrantValidator()
        .AddDeviceAuthentication(options => options.AddUserDeviceStoreEntityFrameworkCore())
        .AddExtendedResourceOwnerPasswordValidator()
        .AddDotnet7CompatibleStores();
        if (webHostEnvironment.IsDevelopment()) {
            IdentityModelEventSource.ShowPII = true;
            identityServerBuilder.AddDeveloperSigningCredential();
        } else {
            var certificate = new X509Certificate2(Path.Combine(webHostEnvironment.ContentRootPath, configuration["IdentityServer:SigningPfxFile"] ?? string.Empty), configuration["IdentityServer:SigningPfxPass"], X509KeyStorageFlags.MachineKeySet);
            identityServerBuilder.AddSigningCredential(certificate);
        }
        return identityServerBuilder;
    }

    private static AuthenticationBuilder AddAuthenticationDefaults(this IServiceCollection services) {
        var authBuilder = services.AddAuthentication();
        static Action<CookieAuthenticationOptions> AuthCookie() => options => {
            options.LoginPath = new PathString("/login");
            options.LogoutPath = new PathString("/logout");
            options.AccessDeniedPath = new PathString("/403");
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        };
        services.ConfigureApplicationCookie(AuthCookie());
        services.ConfigureExtendedValidationCookie(AuthCookie());
        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options => {
            options.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.LoginPath = new PathString("/login-with-2fa");
            options.LogoutPath = new PathString("/logout");
            options.AccessDeniedPath = new PathString("/403");
        });
        return authBuilder;
    }

    /// <summary>Adds Identity server management endpoints dependencies. </summary>
    /// <param name="builder">Builder for configuring the Indice Identity Server.</param>
    /// <param name="configureAction"></param>
    public static IExtendedIdentityServerBuilder AddExtendedEndpoints(this IExtendedIdentityServerBuilder builder, Action<ExtendedEndpointOptions>? configureAction = null) {
        builder.Services.Configure<AntiforgeryOptions>(options => options.HeaderName = CustomHeaderNames.AntiforgeryHeaderName); // Configure anti-forgery token options.
        builder.Services.Configure<ExtendedEndpointOptions>(options => builder.Configuration.GetSection(ExtendedEndpointOptions.Name).Bind(options));
        builder.Services.PostConfigure<ExtendedEndpointOptions>(options => configureAction?.Invoke(options));
        builder.Services.Configure<CacheResourceFilterOptions>(options => builder.Configuration.GetSection(CacheResourceFilterOptions.Name).Bind(options)); // Configure options for CacheResourceFilter.
        builder.Services.PostConfigure<CacheResourceFilterOptions>(options => {
            var endpointOptions = new ExtendedEndpointOptions {
                DisableCache = options.DisableCache
            };
            configureAction?.Invoke(endpointOptions);
            options.DisableCache = endpointOptions.DisableCache;
        });
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.Converters.Add(new JsonStringDecimalConverter());
            options.SerializerOptions.Converters.Add(new JsonStringDoubleConverter());
            options.SerializerOptions.Converters.Add(new JsonStringInt32Converter());
            options.SerializerOptions.Converters.Add(new JsonStringBooleanConverter());
            options.SerializerOptions.Converters.Add(new JsonAnyStringConverter());
            options.SerializerOptions.Converters.Add(new TypeConverterJsonAdapterFactory());
        });
        // The following is unfortunately required in order to have consistent swagger generator.
        builder.Services.Configure<JsonOptions>(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.AddProblemDetails(options => {
            options.CustomizeProblemDetails = ctx => {
                if (ctx.HttpContext.Response.StatusCode == 429) {
                    ctx.ProblemDetails.Title = "Too many requests.";
                    ctx.ProblemDetails.Detail = "Please try again later.";
                    if (ctx.HttpContext.Items.TryGetValue("retry-after", out var retryAfter)) {
                        ctx.ProblemDetails.Detail = $"Please try again after {retryAfter} second(s).";
                    }
                }
            };
        });
        builder.Services.AddOutputCache();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddIdentityRateLimiter(builder.Configuration);
        builder.Services.AddFeatureManagement(builder.Configuration.GetSection("IdentityServer:Features"));
        builder.Services.AddPushNotificationServiceNoop();
        // Invoke action provided by developer to override default options.
        builder.Services.TryAddTransient<IPlatformEventService, PlatformEventService>();
        builder.Services.AddClientThemingService();
        // Add authorization policies that are used by the IdentityServer API.
        builder.Services.AddAuthorization(authOptions => {
            authOptions.AddPolicy(IdentityEndpoints.Scope, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.Scope) || x.User.IsAdmin() || x.User.IsSystemClient());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersReader, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanReadUsers());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersWriter, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanWriteUsers());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersReader, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanReadUsers());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersWriter, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanWriteUsers());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeClientsReader, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Clients) && x.User.CanReadClients());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeClientsWriter, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Clients) && x.User.CanWriteClients());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersOrClientsReader, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && (x.User.CanReadUsers() || x.User.CanReadClients()));
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeAdmin, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.Scope) && (x.User.HasRoleClaim(BasicRoleNames.Administrator) || x.User.HasRoleClaim(BasicRoleNames.AdminUIAdministrator) || x.User.IsAdmin() || x.User.IsSystemClient()));
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeLogsReader, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Logs) && x.User.CanReadUsers());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeLogsWriter, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Logs) && x.User.CanWriteUsers());
            });
            authOptions.AddPolicy(IdentityEndpoints.Policies.BeDeviceAuthenticated, policy => {
                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(context => context.User.HasScope(IdentityEndpoints.Scope) && (context.User.HasClaim(JwtClaimTypes.AuthenticationMethod, CustomGrantTypes.DeviceAuthentication) || context.User.IsAdmin()));
            });
        });
        // Register the authentication handler, using a custom scheme name, for local APIs.
        builder.Services
               .AddAuthentication()
               .AddLocalApi(IdentityEndpoints.AuthenticationScheme, options => {
                   options.ExpectedScope = IdentityEndpoints.Scope;
               });
        return builder;
    }

    /// <summary>Adds all required services for <b>Devices</b> feature.</summary>
    /// <param name="builder">Builder for configuring the Indice Identity Server.</param>
    /// <param name="configureAction">Configuration used for <b>Devices</b> feature.</param>
    public static IExtendedIdentityServerBuilder AddDeviceEndpoints(this IExtendedIdentityServerBuilder builder, Action<DeviceOptions>? configureAction = null) {
        var services = builder.Services;
        var options = new DeviceOptions {
            Services = services
        };
        configureAction?.Invoke(options);
        options.Services = null;
        services.Configure<DeviceOptions>(deviceOptions => {
            deviceOptions.DefaultTotpDeliveryChannel = options.DefaultTotpDeliveryChannel;
        });
        services.AddPushNotificationServiceNoop();
        services.TryAddTransient<IPlatformEventService, PlatformEventService>();
        return builder;
    }

    /// <summary>Adds the required services for the <b>TOTP API</b> features.</summary>
    /// <param name="builder">Builder for configuring the Indice Identity Server.</param>
    public static IExtendedIdentityServerBuilder AddTotpEndpoints(this IExtendedIdentityServerBuilder builder) {
        builder.Services.AddTransient<IValidator<TotpRequest>, TotpRequestValidator>();
        return builder;
    }

    /// <summary>Adds all required services for <b>Database Settings</b> feature.</summary>
    /// <param name="builder">Builder for configuring the Indice Identity Server.</param>
    /// <param name="configureAction">Configuration used for <b>Database Settings</b> feature.</param>
    public static IExtendedIdentityServerBuilder AddDatabaseSettingEndpoints(this IExtendedIdentityServerBuilder builder, Action<SettingsApiOptions>? configureAction = null) {
        var settingsApiOptions = new SettingsApiOptions {
            Services = builder.Services
        };
        configureAction?.Invoke(settingsApiOptions);
        settingsApiOptions.Services = null;
        builder.Services.Configure<SettingsApiOptions>(options => {
            options.ApiPrefix = settingsApiOptions.ApiPrefix;
            options.ApiScope = settingsApiOptions.ApiScope;
        });
        return builder;
    }

    private static IServiceCollection AddIdentityRateLimiter(this IServiceCollection services, IConfiguration configuration) {
        var identityRateLimiterOptions = new IdentityRateLimiterOptions();
        configuration.GetSection(IdentityRateLimiterOptions.SectionName).Bind(identityRateLimiterOptions);
        services.AddRateLimiter(rateLimiterOptions => {
            foreach (var endpoint in IdentityEndpoints.RateLimiter.Endpoints) {
                var endpointOptions = identityRateLimiterOptions.Rules.FirstOrDefault(rule => rule.Endpoint == endpoint) ?? RateLimiterEndpointRule.Default();
                rateLimiterOptions.AddFixedWindowLimiter(endpoint, fixedWindowOptions => {
                    fixedWindowOptions.PermitLimit = endpointOptions.PermitLimit.GetValueOrDefault();
                    fixedWindowOptions.QueueLimit = endpointOptions.QueueLimit.GetValueOrDefault();
                    fixedWindowOptions.QueueProcessingOrder = endpointOptions.QueueProcessingOrder.GetValueOrDefault();
                    fixedWindowOptions.Window = endpointOptions.Window.GetValueOrDefault();
                });
            }
            rateLimiterOptions.RejectionStatusCode = identityRateLimiterOptions.RejectionStatusCode.GetValueOrDefault();
            rateLimiterOptions.OnRejected = (context, cancellationToken) => {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    context.HttpContext.Items.Add("retry-after", retryAfter.TotalSeconds);
                }
                return ValueTask.CompletedTask;
            };
        });
        return services;
    }
}

/// <summary>Extension methods on <see cref="DeviceOptions"/> type.</summary>
public static class DeviceOptionsExtensions
{
    /// <summary>Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.</summary>
    /// <param name="deviceOptions">Options used to configure <b>Devices</b> feature.</param>
    /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
    public static void UsePushNotificationsServiceAzure(this DeviceOptions deviceOptions, Action<IServiceProvider, PushNotificationAzureOptions>? configure = null) =>
        deviceOptions.Services.AddPushNotificationServiceAzure(configure);
}
