using System;
using IdentityServer4;
using IdentityServer4.Infrastructure;
using Indice.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthenticationConfig
    {
        public static IServiceCollection AddAuthenticationConfig(this IServiceCollection services, IConfiguration configuration) {
            var authBuilder = services.AddAuthentication();
            static Action<CookieAuthenticationOptions> AuthCookie() => options => {
                options.AccessDeniedPath = new PathString("/access-denied");
                options.LoginPath = new PathString("/login");
                options.LogoutPath = new PathString("/logout");
            };
            services.ConfigureApplicationCookie(AuthCookie());
            var microsoftAuthSettings = configuration.GetSection($"Auth:{MicrosoftAccountDefaults.AuthenticationScheme}").Get<ClientSettings>();
            if (!string.IsNullOrEmpty(microsoftAuthSettings.ClientId) && !string.IsNullOrEmpty(microsoftAuthSettings.ClientSecret)) {
                var serviceProvider = services.BuildServiceProvider();
                authBuilder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, "Connect with Microsoft", options => {
                    options.AuthorizationEndpoint = string.IsNullOrWhiteSpace(microsoftAuthSettings.TenantId) 
                        ? MicrosoftAccountDefaults.AuthorizationEndpoint 
                        : $"https://login.microsoftonline.com/{microsoftAuthSettings.TenantId}/oauth2/v2.0/authorize";
                    options.TokenEndpoint = string.IsNullOrWhiteSpace(microsoftAuthSettings.TenantId)
                        ? MicrosoftAccountDefaults.TokenEndpoint
                        : $"https://login.microsoftonline.com/{microsoftAuthSettings.TenantId}/oauth2/v2.0/token";
                    options.ClientId = microsoftAuthSettings.ClientId;
                    options.ClientSecret = microsoftAuthSettings.ClientSecret;
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.StateDataFormat = new DistributedCacheStateDataFormatter(serviceProvider.GetService<IHttpContextAccessor>(), MicrosoftAccountDefaults.AuthenticationScheme);
                });
            }
            return services;
        }
    }
}
