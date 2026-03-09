using System.Linq;
using System.Security.Claims;
using System.Xml.Linq;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>Extension methods to configure GovGr OAuth authentication.</summary>
public static class GovGrExtensions
{
    /// <summary>
    /// Adds GovGr OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme. The default scheme is specified by <see cref="GovGrDefaults.AuthenticationScheme"/>.
    /// <para>
    /// GovGr authentication allows application users to sign in with their GovGr account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGovGr(this AuthenticationBuilder builder, Action<GovGrOptions> configureOptions) => builder.AddGovGr(GovGrDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds GovGr OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme. The default scheme is specified by <see cref="GovGrDefaults.AuthenticationScheme"/>.
    /// <para>
    /// GovGr authentication allows application users to sign in with their GovGr account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGovGr(this AuthenticationBuilder builder, string authenticationScheme, Action<GovGrOptions> configureOptions) => builder.AddGovGr(authenticationScheme, GovGrDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Adds GovGr OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme. The default scheme is specified by <see cref="GovGrDefaults.AuthenticationScheme"/>.
    /// <para>
    /// GovGr authentication allows application users to sign in with their GovGr account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGovGrKyc(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GovGrOptionsKyc> configureOptions)
        => builder.AddOpenIdConnect(authenticationScheme, displayName, (options) => {
            var govGrOptions = new GovGrOptionsKyc();
            configureOptions?.Invoke(govGrOptions);
            if (string.IsNullOrWhiteSpace(govGrOptions.ClientId)) {
                throw new ArgumentOutOfRangeException(nameof(govGrOptions.ClientId), "GovGr Id. The '{0}' option must be provided.");
            }
            if (string.IsNullOrWhiteSpace(govGrOptions.ClientSecret)) {
                throw new ArgumentOutOfRangeException(nameof(govGrOptions.ClientSecret), "GovGr Id. The '{0}' option must be provided.");
            }
            // Manually set these two endpoint since there is not a well known configuration endpoint.
            options.Configuration = new OpenIdConnectConfiguration {
                TokenEndpoint = govGrOptions.TokenEndpoint,
                AuthorizationEndpoint = govGrOptions.AuthorizationEndpoint,
                UserInfoEndpoint = govGrOptions.UserInfoEndpoint
            };
            options.SaveTokens = true;
            options.Authority = govGrOptions.Authority;
            options.CallbackPath = govGrOptions.CallbackPath ?? new PathString("/signin-govgr");
            options.SignInScheme = govGrOptions.SignInScheme ?? CookieAuthenticationDefaults.AuthenticationScheme;
            options.ResponseType = "code id_token";
            options.DisableTelemetry = true;
            options.SaveTokens = true;
            options.Scope.Clear();
            options.MapInboundClaims = false;
            foreach (var scope in govGrOptions.Scopes) {
                options.Scope.Add(scope);
            }
            options.ClientId = govGrOptions.ClientId;
            options.ClientSecret = govGrOptions.ClientSecret;
            options.UsePkce = false;
        });



    /// <summary>
    /// Adds GovGr OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme. The default scheme is specified by <see cref="GovGrDefaults.AuthenticationScheme"/>.
    /// <para>
    /// GovGr authentication allows application users to sign in with their GovGr account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="GovGrOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGovGr(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GovGrOptions> configureOptions) {

        builder.AddOAuth<GovGrOptions, GovGrHandler>(authenticationScheme, displayName, (options) => {
            ApplyGovGrDefaults(options, authenticationScheme);
            configureOptions?.Invoke(options);
        });
        builder.Services.AddHttpClient(authenticationScheme, (sp, httpClient) => {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OAuth handler");
            httpClient.Timeout = sp.GetRequiredService<IOptionsMonitor<GovGrOptions>>().Get(authenticationScheme).BackchannelTimeout;
            httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
        });
        return builder;
    }

    internal static readonly string GovGrClaimNullLiteral = "null";
    internal static readonly Dictionary<string, string> GovGrClaimMap = new() {
        ["userid"] = BasicClaimTypes.Name,
        ["firstname"] = BasicClaimTypes.GivenName,
        ["lastname"] = BasicClaimTypes.FamilyName,
        ["fathername"] = "father_name",
        ["mothername"] = "mother_name",
        ["birthyear"] = BasicClaimTypes.BirthDate,
        ["taxid"] = BasicClaimTypes.Tin,
    };

    private static void ApplyGovGrDefaults(GovGrOptions options, string authenticationScheme) {
        options.SaveTokens = true;
        options.CallbackPath = new PathString("/signin-govgr");
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.UsePkce = false;
        options.EnableFederatedLogout = true;
        options.Scope.Clear();
        options.Scope.Add(GovGrScope.Read);
    }

    private static void SetGovGrEndpoints(GovGrOptions options) {
        options.AuthorizationEndpoint = $"{options.Authority}/oauth/authorize";
        options.TokenEndpoint = $"{options.Authority}/oauth/token";
        options.UserInformationEndpoint = $"{options.Authority}/userinfo?format=xml";
    }
}
