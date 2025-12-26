using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Indice.Security;
using Indice.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// A set of extension methods to configure the <see cref="IndiceWebApplicationBuilder"/> both pipeline and dependencies. 
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the builder for minimal api host.
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for further configuration.</returns>
    /// <remarks>Here we AddLocalization, AddCors, AddAuthentication (Jwt, Introspection), AddAutorizationCore,  AddEndpointsApiExplorer, ConfigureHttpJsonOptions, AddProblemDetails, AddEndpointParameterFluentValidation, AddGeneralSettings</remarks>
    public static WebApplicationBuilder AddMinimalApiDefaults(this WebApplicationBuilder builder) {
        builder.Services.AddLocalization(options => {
            options.ResourcesPath = "Resources";
        });
        builder.Services.AddCors(options => options.AddDefaultPolicy(policyBuilder => {
            policyBuilder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders(HeaderNames.ContentDisposition);
        }));
        builder.AddApiAuthenticationDefaults();
        builder.AddApiAuthorizationDefaults();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // used by swashbucle!
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
        // Configure indice services
        builder.Services.AddProblemDetails(options => {
            options.CustomizeProblemDetails = context => {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
                Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
            };
        });
        builder.Services.AddEndpointParameterFluentValidation(Assembly.GetEntryAssembly()!);
        builder.Services.AddGeneralSettings(builder.Configuration);
        builder.AddProxyDefaults();
        return builder;
    }

    /// <summary>
    /// Adds api authentication defaults
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for further configuration.</returns>
    public static AuthenticationBuilder AddApiAuthenticationDefaults(this WebApplicationBuilder builder) {
        // Clear mapping of the inbound standard claims to Microsoft's proprietary ones.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        var authBuilder = builder.Services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        // JWT tokens (default scheme).
        .AddJwtBearer(options => {
            options.Authority = builder.Configuration.GetAuthority();
            options.MetadataAddress = builder.Configuration.GetAuthorityMetadata(tryInternal: true);
            options.Audience = builder.Configuration.GetApiResourceName() ?? "api1";
            options.TokenValidationParameters.RoleClaimType = JwtClaimTypes.Role;
            options.TokenValidationParameters.NameClaimType = JwtClaimTypes.Name;
            options.RequireHttpsMetadata = false;
            options.MapInboundClaims = false;
            // if token does not contain a dot, it is a reference token.
            options.ForwardDefaultSelector = BearerSelector.ForwardReferenceToken("Introspection");
        })
        // Reference tokens.
        .AddOAuth2Introspection("Introspection", options => {
            // Base address of the Identity Server.
            options.Authority = builder.Configuration.GetAuthority(tryInternal: true);
            options.DiscoveryPolicy = new DiscoveryPolicy() {
                Authority = builder.Configuration.GetAuthority(tryInternal: true)!,
                AdditionalEndpointBaseAddresses = [builder.Configuration.GetAuthority()!],
                ValidateIssuerName = false,
                RequireHttps = false,
            };
            // Name of the API resource.
            options.ClientId = builder.Configuration.GetApiResourceName() ?? "api1";
            // Since the introspection endpoint requires authentication, we need to supply the configured API secret.
            options.ClientSecret = builder.Configuration.GetApiSecret("Introspection");
            // Enable caching so we avoid perform a round-trip to the introspection endpoint for each incoming request. 
            options.EnableCaching = true;
            // Having multiple instances of the same application with distributed cache configured (Redis, Sql) is not an issue.
            // Having same distributed cache configuration for difference applications, regardless of instance count, is a problem.
            // Add cache key to avoid caching the same token for different api hosts
            options.CacheKeyPrefix = $"{options.ClientId}_";
            options.CacheDuration = TimeSpan.FromMinutes(5); // 5 minutes is the default. Should potentially go back up to defaults.
        });
        builder.Services.AddScopeTransformation();
        return authBuilder;
    }

    /// <summary>
    /// Adds api authorization defaults
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for further configuration.</returns>
    /// <remarks>Only adds the <strong>BeAdmin</strong> policy that checks for <strong>admin</strong> user claim or the <strong>system</strong> client claim</remarks>
    public static WebApplicationBuilder AddApiAuthorizationDefaults(this WebApplicationBuilder builder) {
        builder.Services.AddAuthorization(authOptions => {
            // Allow actions for admins.
            authOptions.AddPolicy("BeAdmin", policy => {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(x => x.User.IsAdmin() || x.User.IsSystemClient());
            });
        });
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="ForwardedHeadersMiddleware"/> proxy IPs or CIDR ranges for trusting forwarded headers. 
    /// This usually applies when the application is hosted behind a reverse proxy server such as an Kubernetes ingress controller or Azure Front Door/Cloudflare.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> instance.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for chaining.</returns>
    /// <remarks>Reads the following configuration values: 
    /// <strong>Proxy:Enabled</strong> (bool), 
    /// <strong>Proxy:KnownNetworks</strong> (string comma delimited), 
    /// <strong>Proxy:KnownProxies</strong> (string comma delimited), 
    /// <strong>Proxy:ForwardLimit</strong> (int)</remarks>
    public static WebApplicationBuilder AddProxyDefaults(this WebApplicationBuilder builder) {
        builder.Services.AddProxyForwardedHeaders(builder.Configuration);
        return builder;
    }
}
