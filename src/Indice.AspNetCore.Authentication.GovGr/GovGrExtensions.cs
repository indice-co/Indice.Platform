using System.Linq;
using System.Security.Claims;
using System.Xml.Linq;
using Indice.Features.GovGr.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
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
        builder.Services.AddHttpClient(authenticationScheme, (sp, httpClient) => {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OAuth handler");
            //httpClient.Timeout = sp.GetRequiredService<IOptionsMonitor<GovGrOptions>>().Get(authenticationScheme).BackchannelTimeout;
            httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
        });

        builder.Services.AddOptions<GovGrOptions>(authenticationScheme)
                        .Configure<IHttpClientFactory>((options, httpClientFactory) => {
                            ApplyGovGrDefaults(options, authenticationScheme, httpClientFactory);
                            configureOptions?.Invoke(options);
                            var baseOptions = (OAuthOptions)options;
                            baseOptions.AuthorizationEndpoint = options.AuthorizationEndpoint;
                            baseOptions.TokenEndpoint = options.TokenEndpoint;
                            baseOptions.UserInformationEndpoint = options.UserInfoEndpoint;

                        });


        return builder.AddOAuth<GovGrOptions, GovGrHandler>(authenticationScheme, displayName, (options) => { });
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

    private static void ApplyGovGrDefaults(GovGrOptions options,string authenticationScheme,IHttpClientFactory httpClientFactory) {

        options.SaveTokens = true;
        options.CallbackPath =  new PathString("/signin-govgr");
        options.SignInScheme =  CookieAuthenticationDefaults.AuthenticationScheme;
        //options.Scope.Clear();
        //foreach (var scope in govGrOptions.Scopes) {
        //    options.Scope.Add(scope);
        //}
        options.UserInformationEndpoint = options.UserInfoEndpoint;
        options.UsePkce = false;
        options.EnableFederatedLogout = true;
        //options.Scope.Clear();
        // CRITICAL: Push the computed endpoints down to the base OAuthOptions.
        // Without this, the underlying OAuth handler will read null endpoints.
        var baseOptions = (OAuthOptions)options;
        baseOptions.AuthorizationEndpoint = options.AuthorizationEndpoint;
        baseOptions.TokenEndpoint = options.TokenEndpoint;
        baseOptions.UserInformationEndpoint = options.UserInfoEndpoint;

        options.Backchannel = httpClientFactory.CreateClient(authenticationScheme!);
        options.Backchannel.Timeout = options.BackchannelTimeout;
        options.BackchannelHttpHandler = null;
        options.Events.OnCreatingTicket = async (context) => {
            var accessToken = context.Properties.GetTokenValue("access_token");
            var httpClient = context.Backchannel;
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var xml = XDocument.Parse(responseBody);
            var claims = xml.Descendants("userinfo")
                            .SelectMany(x => x.Attributes()
                                              .Select(attr => new Claim(GovGrExtensions.GovGrClaimMap.GetValueOrDefault(attr.Name.LocalName, attr.Name.LocalName), attr.Value.Trim())))
                            .Where(x => !GovGrExtensions.GovGrClaimNullLiteral.Equals(x.Value, StringComparison.OrdinalIgnoreCase))
                            .ToList();
            // add another claim for subject since this is not available.
            claims.Add(new Claim(BasicClaimTypes.Subject, claims.Find(x => x.Type == BasicClaimTypes.Name)!.Value));
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name, BasicClaimTypes.Name, BasicClaimTypes.Role));
        };
    }
}

/// <summary>
/// Configures OAuth authentication options for integration with the GovGr identity provider.
/// </summary>
/// <remarks>This class is typically used to bind GovGr-specific settings to an instance of <see
/// cref="GovGrOptions"/> for use with ASP.NET Core authentication. It ensures that required GovGr endpoints and
/// credentials are set and maps user information claims according to GovGr's schema. This class is intended for use
/// with dependency injection and is not thread-safe for direct use across multiple authentication schemes.</remarks>
//public class ConfigureGovGrOptions : IConfigureNamedOptions<GovGrOptions>, IConfigureOptions<GovGrOptions>
//{
//    private readonly IHttpClientFactory _httpClientFactory;
//    /// <summary>
//    /// Initializes a new instance of the ConfigureGovGrOptions class using the specified options factory.
//    /// </summary>
//    /// <param name="httpClientFactory"> The factory used to create HTTP client instances for back-channel OAuth communication. Cannot be null.</param>
//    /// <param name="govGrOptionsFactory">The factory used to create instances of GovGrOptions. Cannot be null.</param>
//    public ConfigureGovGrOptions(IHttpClientFactory httpClientFactory, IOptionsFactory<GovGrOptions> govGrOptionsFactory) {
//        _httpClientFactory = httpClientFactory;
//        GovGrOptionsFactory = govGrOptionsFactory;
//    }

//    /// <summary>
//    /// Gets the factory used to create configured instances of <see cref="GovGrOptions"/>.
//    /// </summary>
//    public IOptionsFactory<GovGrOptions> GovGrOptionsFactory { get; }

//    /// <inheritdoc />
//    public void Configure(string? name, GovGrOptions options) {
//        var govGrOptions = GovGrOptionsFactory.Create(name!);
//        if (string.IsNullOrWhiteSpace(govGrOptions.ClientId)) {
//            throw new ArgumentOutOfRangeException(nameof(govGrOptions.ClientId), "GovGr Id. The '{0}' option must be provided.");
//        }
//        if (string.IsNullOrWhiteSpace(govGrOptions.ClientSecret)) {
//            throw new ArgumentOutOfRangeException(nameof(govGrOptions.ClientSecret), "GovGr Id. The '{0}' option must be provided.");
//        }
//        // Manually set these two endpoint since there is not a well known configuration endpoint.
//        options.TokenEndpoint = govGrOptions.TokenEndpoint;
//        options.AuthorizationEndpoint = govGrOptions.AuthorizationEndpoint;
//        options.UserInformationEndpoint = govGrOptions.UserInfoEndpoint;
//        options.LogoutEndpoint = govGrOptions.LogoutEndpoint;
//        options.SaveTokens = true;
//        options.CallbackPath = govGrOptions.CallbackPath ?? new PathString("/signin-govgr");
//        options.SignInScheme = govGrOptions.SignInScheme ?? CookieAuthenticationDefaults.AuthenticationScheme;
//        options.Scope.Clear();
//        foreach (var scope in govGrOptions.Scopes) {
//            options.Scope.Add(scope);
//        }
//        options.ClientId = govGrOptions.ClientId;
//        options.ClientSecret = govGrOptions.ClientSecret;
//        options.UsePkce = false;
//        options.BackchannelTimeout = govGrOptions.BackchannelTimeout;
//        options.Backchannel = _httpClientFactory.CreateClient(name!);
//        options.BackchannelHttpHandler = null;
//        options.EnableFederatedLogout = govGrOptions.EnableFederatedLogout;
//        if (govGrOptions.Events?.OnRemoteFailure is not null) {
//            options.Events.OnRemoteFailure = govGrOptions.Events.OnRemoteFailure;
//        }
//        options.Events.OnCreatingTicket = async (context) => {
//            var accessToken = context.Properties.GetTokenValue("access_token");
//            var httpClient = context.Backchannel;
//            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
//            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
//            var response = await httpClient.SendAsync(request);
//            response.EnsureSuccessStatusCode();
//            var responseBody = await response.Content.ReadAsStringAsync();
//            var xml = XDocument.Parse(responseBody);
//            var claims = xml.Descendants("userinfo")
//                            .SelectMany(x => x.Attributes()
//                                              .Select(attr => new Claim(GovGrExtensions.GovGrClaimMap.GetValueOrDefault(attr.Name.LocalName, attr.Name.LocalName), attr.Value.Trim())))
//                            .Where(x => !GovGrExtensions.GovGrClaimNullLiteral.Equals(x.Value, StringComparison.OrdinalIgnoreCase))
//                            .ToList();
//            // add another claim for subject since this is not available.
//            claims.Add(new Claim(BasicClaimTypes.Subject, claims.Find(x => x.Type == BasicClaimTypes.Name)!.Value));
//            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name, BasicClaimTypes.Name, BasicClaimTypes.Role));
//        };
//    }

//    /// <inheritdoc />
//    public void Configure(GovGrOptions options) {
//        Configure(GovGrDefaults.AuthenticationScheme, options);
//    }

//}