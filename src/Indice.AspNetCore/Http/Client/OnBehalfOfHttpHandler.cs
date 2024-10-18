#nullable enable
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Indice.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Http.Client;

/// <summary>
/// A function that when called will return the value of the current caller access_token. 
/// </summary>
/// <returns>The access token on behalf of the relay operation will take place</returns>
/// <remarks>Usually this would be populated with a function that locates the current Httpcontext and extract the Bearer token from the authorize header</remarks>
public delegate string? OnBehalfOfAccessTokenSelector();
/// <summary>
/// A function that when called will return the value of the current caller subject id or userid. 
/// </summary>
/// <returns>The current user id/subject id</returns>
/// <remarks>Usually this would be populated with a function that locates the current Httpcontext and extracts <strong>sub</strong> claim for the current claims principal</remarks>
public delegate string? OnBehalfOfSubjectIdSelector();

/// <summary>
/// The options used to configure a <see cref="OnBehalfOfHttpHandler"/>
/// </summary>
public class OnBehalfOfHttpHandlerOptions
{
    /// <summary>The default httpclient name that will be used if an alternative is not configured.</summary>
    /// <remarks>Configure the an http client with the same name in case we need to override the default httpclient behavior</remarks>
    public const string DefaultTokenClientName = "DelegationHttpClient";
    /// <summary>
    /// The authentication scheme to use in order to retrieve the access_token of the current user. Defaults to <strong>Bearer</strong>
    /// </summary>
    public const string AuthenticationScheme = "Bearer";
    /// <summary>
    /// The client id to use to connect to the Identity server and excehange the current user token.
    /// </summary>
    /// <remarks>This client must be assigned the delegation grant</remarks>
    public string ClientId { get; set; } = null!;
    /// <summary>the client secret</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The scope that the new token will expose.</summary>
    /// <remarks>usually this scope would not be available to the incomming client request and thus there is the need for delegation or client_credentials.</remarks>
    public string Scope { get; set; } = null!;
    /// <summary>
    /// The endpoint new access token will be issued. For example https://youridentity.com/connect/token or if there is an http client configured with a base uri set thenb simply pass /connect/toke
    /// </summary>
    /// <remarks>Defaults to <strong>/connect/token</strong></remarks>
    public string TokenEndpoint { get; set; } = "/connect/token";
    /// <summary>
    /// A function that when called will return the value of the current caller access_token. 
    /// </summary>
    /// <remarks>Usually this would be populated with a function that locates the current Httpcontext and extract the Bearer token from the authorize header</remarks>
    internal OnBehalfOfAccessTokenSelector AccessTokenSelector { get; set; } = null!;
    /// <summary>
    /// A function that when called will return the value of the current caller subject id or userid. 
    /// </summary>
    /// <remarks>Usually this would be populated with a function that locates the current Httpcontext and extracts <strong>sub</strong> claim for the current claims principal</remarks>
    internal OnBehalfOfSubjectIdSelector SubjectIdSelector { get; set; } = null!;
    /// <summary>The HttpClient name to use when resolving the <seealso cref="HttpClient"/> to use.</summary>
    /// <remarks>It is important that the client has the base uri configured to the correct authority if the <seealso cref="TokenEndpoint"/> is not an absolute path.</remarks>
    internal string TokenClientName { get; set; } = DefaultTokenClientName;
}

/// <summary>
/// Use the <see cref="OnBehalfOfHttpHandler"/> to automatically resolve accesstokens <strong>on behalf of</strong> the current caller/user 
/// so that the current process can elevate its priviledges in order to fuillfil the requirements.
/// </summary>
public class OnBehalfOfHttpHandler : DelegatingHandler
{
    private readonly OnBehalfOfHttpHandlerOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly string _ApplicationName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="cache"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public OnBehalfOfHttpHandler(OnBehalfOfHttpHandlerOptions options, IHttpClientFactory httpClientFactory, IDistributedCache cache) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory;
        _ApplicationName = Assembly.GetEntryAssembly()!.GetName()!.Name!;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var accessToken = await GetDelegatedAccessToken();

        request.SetToken(OnBehalfOfHttpHandlerOptions.AuthenticationScheme, accessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetDelegatedAccessToken() {
        var currentAccessToken = _options.AccessTokenSelector();
        var currentSubjectId = _options.SubjectIdSelector();
        var userPresent = !string.IsNullOrWhiteSpace(currentAccessToken) && !string.IsNullOrWhiteSpace(currentSubjectId);
        var grantType = userPresent ? "delegation" : "client_credentials";
        var cacheKey = $"{_ApplicationName}|{_options.ClientId}|{grantType}|sub|{currentSubjectId}";
        var accessToken = await _cache.GetStringAsync(cacheKey);
        if (accessToken != null) {
            return accessToken;
        }
        var response = await GetOnBehalfOfOrClientCredentialsTokenResponse(currentAccessToken, userPresent: userPresent);
        await _cache.SetStringAsync(cacheKey, response.AccessToken!, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 30)
        });
        return response.AccessToken!;
    }

    private async Task<TokenResponse> GetOnBehalfOfOrClientCredentialsTokenResponse(string? userToken, bool userPresent) {
        var httpClient = _httpClientFactory.CreateClient(_options.TokenClientName);
        var grantType = "client_credentials";
        Parameters parameters = [
            new ("scope", _options.Scope),
        ];
        if (userPresent) {
            grantType = "delegation";
            parameters = [new("scope", _options.Scope),
                          new("token", userToken!)];
        }
        var response = await httpClient.RequestTokenAsync(new TokenRequest {
            Address = _options.TokenEndpoint,
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            GrantType = grantType,
            Parameters = parameters
        });
        if (response.IsError) {
            throw new OnBehalfOfHttpHandlerException(response.Error, response.Exception);
        }
        return response;
    }
}

/// <summary>
/// <see cref="OnBehalfOfHttpHandler"/> exception. This is thrown when the delegation grant handler fails to issue a fresh token.
/// </summary>
public class OnBehalfOfHttpHandlerException : Exception
{
    /// <inheritdoc/>
    public OnBehalfOfHttpHandlerException(string? message) : base(message) {

    }
    /// <inheritdoc/>
    public OnBehalfOfHttpHandlerException(string? message, Exception? innerException) : base(message, innerException) {

    }
}

/// <summary>
/// Extenstion methods for registering and configuring the <see cref="OnBehalfOfHttpHandler"/> and all its dependencies.
/// </summary>
public static class OnBehalfOfHttpHandlerExtensions
{
    /// <summary>
    /// Gets the access token from the current value of the <strong>Authorization</strong> header next to the scheme.
    /// </summary>
    /// <param name="httpContext">The current httpContext</param>
    /// <returns>The access token located or null</returns>
    public static string? ResolveAuthorizationHeaderValue(this HttpContext httpContext) {
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
            return authHeader["Bearer ".Length..];
        }

        return null;
    }

    /// <summary>
    /// Add the on behalf of functionality to the current <see cref="IHttpClientBuilder"/>. The the <seealso cref="OnBehalfOfHttpHandler"/> will be added to the message pipeline
    /// and automatically elevate the current users context.
    /// </summary>
    /// <param name="httpClientBuilder">The client builder to confiugure</param>
    /// <param name="tokenClientName">The http client name to use in order to make the calls to the authority (token endpoint)</param>
    /// <param name="configureAction">Configures the available options.</param>
    /// <returns>The builder for further configuration.</returns>
    public static IHttpClientBuilder AddOnBehalfOfTokenHandler(this IHttpClientBuilder httpClientBuilder, string tokenClientName, Action<OnBehalfOfHttpHandlerOptions> configureAction) {

        httpClientBuilder.Services.TryAddTransient<OnBehalfOfAccessTokenSelector>(sp => new(() => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.ResolveAuthorizationHeaderValue()));
        httpClientBuilder.Services.TryAddTransient<OnBehalfOfSubjectIdSelector>(sp => new(() => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User.FindSubjectId()));
        httpClientBuilder.Services.Configure(configureAction);
        httpClientBuilder.Services.TryAddTransient(sp => {
            var options = sp.GetRequiredService<IOptions<OnBehalfOfHttpHandlerOptions>>().Value;
            options.AccessTokenSelector = sp.GetRequiredService<OnBehalfOfAccessTokenSelector>();
            options.SubjectIdSelector = sp.GetRequiredService<OnBehalfOfSubjectIdSelector>();
            options.TokenClientName = tokenClientName;
            return options;
        });
        httpClientBuilder.Services.TryAddTransient<OnBehalfOfHttpHandler>();
        return httpClientBuilder.AddHttpMessageHandler<OnBehalfOfHttpHandler>();
    }

    /// <summary>
    /// Add the on behalf of functionality to the current <see cref="IHttpClientBuilder"/>. The the <seealso cref="OnBehalfOfHttpHandler"/> will be added to the message pipeline
    /// and automatically elevate the current users context.
    /// </summary>
    /// <param name="httpClientBuilder">The client builder to confiugure</param>
    /// <param name="tokenClientName">The http client name to use in order to make the calls to the authority (token endpoint)</param>
    /// <param name="configuration">Will use the configuration object and pick the default values in order to pass parameters regarding the current client credentials and scopes.
    /// This uses <strong>General:Authority</strong>, <strong>General:Api:Secrets:ClientId</strong>, <strong>General:Api:Secrets:ClientSecret</strong>, <strong>General:Api:Secrets:ClientScope</strong>
    /// </param>
    /// <returns>The builder for further configuration.</returns>
    public static IHttpClientBuilder AddOnBehlafOfTokenHandler(this IHttpClientBuilder httpClientBuilder, string tokenClientName, IConfiguration configuration) =>
        httpClientBuilder.AddOnBehalfOfTokenHandler(tokenClientName, (options) => {
            var apiSecrets = configuration.GetApiSecrets();
            options.ClientId = apiSecrets["ClientId"];
            options.ClientSecret = apiSecrets["ClientSecret"];
            options.Scope = apiSecrets.ContainsKey("DelegationScope") ? apiSecrets["DelegationScope"] : apiSecrets["ClientScope"];
            options.TokenEndpoint = $"{configuration.GetAuthority(tryInternal: true).TrimEnd('/')}/connect/token";
        });
}
#nullable disable