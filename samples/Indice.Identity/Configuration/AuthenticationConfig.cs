using IdentityServer4;
using IdentityServer4.Infrastructure;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Indice.AspNetCore.Authentication.Apple;
using Indice.AspNetCore.Authentication.GovGr;
using Indice.AspNetCore.Identity;
using Indice.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace Microsoft.Extensions.DependencyInjection;

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
        services.Configure(ExtendedIdentityConstants.TwoFactorUserIdScheme, AuthCookie());
        var microsoftAuthSettings = configuration.GetSection($"Auth:{MicrosoftAccountDefaults.AuthenticationScheme}").Get<ClientSettings>();
        if (!string.IsNullOrEmpty(microsoftAuthSettings?.ClientId) && !string.IsNullOrEmpty(microsoftAuthSettings?.ClientSecret)) {
            var serviceProvider = services.BuildServiceProvider();
            authBuilder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, "Connect with Microsoft", options => {
                options.AuthorizationEndpoint = string.IsNullOrWhiteSpace(microsoftAuthSettings.TenantId) ? MicrosoftAccountDefaults.AuthorizationEndpoint : $"https://login.microsoftonline.com/{microsoftAuthSettings.TenantId}/oauth2/v2.0/authorize";
                options.TokenEndpoint = string.IsNullOrWhiteSpace(microsoftAuthSettings.TenantId) ? MicrosoftAccountDefaults.TokenEndpoint : $"https://login.microsoftonline.com/{microsoftAuthSettings.TenantId}/oauth2/v2.0/token";
                options.ClientId = microsoftAuthSettings.ClientId;
                options.ClientSecret = microsoftAuthSettings.ClientSecret;
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.SaveTokens = true;
                options.StateDataFormat = new DistributedCacheStateDataFormatter(serviceProvider.GetService<IHttpContextAccessor>(), MicrosoftAccountDefaults.AuthenticationScheme);
            });
        }
        var appleSettings = configuration.GetSection($"Auth:{AppleDefaults.AuthenticationScheme}").Get<AppleOptions>();
        if (!string.IsNullOrEmpty(appleSettings?.ServiceId) && !string.IsNullOrEmpty(appleSettings?.PrivateKey)) {
            authBuilder.AddAppleID(AppleDefaults.AuthenticationScheme, options => {
                options.ServiceId = appleSettings.ServiceId;
                options.TeamId = appleSettings.TeamId;
                options.PrivateKey = appleSettings.PrivateKey;
                options.PrivateKeyId = appleSettings.PrivateKeyId;
                options.Events.OnRemoteFailure = HandleOnRemoteFailure;
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
            });
        }
        var govGrSettings = configuration.GetSection($"Auth:{GovGrDefaults.AuthenticationScheme}").Get<GovGrOptions>();
        if (!string.IsNullOrEmpty(govGrSettings?.ClientId) && !string.IsNullOrEmpty(govGrSettings?.ClientSecret)) {
            authBuilder.AddGovGr(GovGrDefaults.AuthenticationScheme, options => {
                options.ClientId = govGrSettings.ClientId;
                options.ClientSecret = govGrSettings.ClientSecret;
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                if (!string.IsNullOrWhiteSpace(govGrSettings.CallbackPath)) {
                    options.CallbackPath = govGrSettings.CallbackPath;
                }
            });
        }
        return services;
    }

    private static async Task HandleOnRemoteFailure(RemoteFailureContext context) {
        var error = context.Failure.Data["error"];
        var userCancelled = error == null || (string)error == "user_cancelled_authorize";
        if (userCancelled) {
            var interactionService = context.HttpContext.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
            if (context.Properties.Items.TryGetValue("returnUrl", out var returnUrl)) {
                var authorizationContext = await interactionService.GetAuthorizationContextAsync(returnUrl);
                if (authorizationContext != null) {
                    await interactionService.DenyAuthorizationAsync(authorizationContext, AuthorizationError.AccessDenied);
                    context.Response.Redirect(returnUrl);
                }
            }
        }
        context.HandleResponse();
    }
}
