using System;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using Indice.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthenticationConfig
    {
        public static IServiceCollection AddAuthenticationConfig(this IServiceCollection services, GeneralSettings generalSettings) {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var authenticationBuilder = services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => {
                options.Audience = generalSettings?.Api?.ResourceName;
                options.Authority = generalSettings?.Authority;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.NameClaimType = JwtClaimTypes.Name;
                options.TokenValidationParameters.RoleClaimType = JwtClaimTypes.Role;
                options.ForwardDefaultSelector = BearerSelector.ForwardReferenceToken("Introspection");
            })
            .AddOAuth2Introspection("Introspection", options => {
                options.Authority = generalSettings?.Authority;
                options.CacheDuration = TimeSpan.FromMinutes(5);
                options.ClientId = generalSettings?.Api?.ResourceName;
                options.ClientSecret = generalSettings?.Api?.Secrets["Introspection"];
                options.EnableCaching = true;
            });
            services.AddScopeTransformation();
            return services;
        }
    }
}
