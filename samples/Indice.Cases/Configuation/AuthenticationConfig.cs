using System.IdentityModel.Tokens.Jwt;
using Indice.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Indice.Cases.Configuation;

public static class AuthenticationConfig
{
    public static AuthenticationBuilder AddAuthenticationConfig(this IServiceCollection services, GeneralSettings settings) {
        // Clear mapping of the inbound standard claims to Microsoft's proprietary ones.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        return services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddIdentityServerAuthentication(options => {
                // Base address of the Identity Server.
                options.Authority = settings?.Authority;
                // Name of the API resource.
                options.ApiName = settings?.Api?.ResourceName ?? "cases-api";
                // If the incoming token is not a JWT, the middleware will contact the introspection endpoint found in the discovery document to validate the token. 
                // Since the introspection endpoint requires authentication, we need to supply the configured API secret.
                options.ApiSecret = settings?.Api?.Secrets["Introspection"];
                options.RequireHttpsMetadata = false;
                // Enable caching so we avoid perform a roundtrip to the introspection endpoint for each incoming request. 
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromMinutes(1); // 5 mins is the default. Should potentialy go back up to defaults.
            });
    }
}
