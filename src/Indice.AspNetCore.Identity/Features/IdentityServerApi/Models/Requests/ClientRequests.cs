using System.Collections.Generic;
using IdentityServer4.Models;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Models a client that will be created on the server.</summary>
public class CreateClientRequest : BaseClientRequest
{
    /// <summary>Describes the type of the client.</summary>
    public ClientType ClientType { get; set; }
    /// <summary>The unique identifier for this application.</summary>
    public string ClientId { get; set; }
    /// <summary>Allowed URL to return after logging in.</summary>
    public string RedirectUri { get; set; }
    /// <summary>Allowed URL to return after logout.</summary>
    public string PostLogoutRedirectUri { get; set; }
    /// <summary>The client secrets.</summary>
    public List<CreateSecretRequest> Secrets { get; set; } = new List<CreateSecretRequest>();
    /// <summary>The list of identity resources allowed by the client.</summary>
    public IEnumerable<string> IdentityResources { get; set; } = new List<string>();
    /// <summary>The list of API resources allowed by the client.</summary>
    public IEnumerable<string> ApiResources { get; set; } = new List<string>();
}

/// <summary>Models a client that will be updated on the server.</summary>
public class UpdateClientRequest : BaseClientRequest
{
    /// <summary>Lifetime of identity token in seconds.</summary>
    public int IdentityTokenLifetime { get; set; }
    /// <summary>Lifetime of access token in seconds</summary>
    public int AccessTokenLifetime { get; set; }
    /// <summary>Maximum lifetime of a refresh token in seconds.</summary>
    public int AbsoluteRefreshTokenLifetime { get; set; }
    /// <summary>Lifetime of a user consent in seconds.</summary>
    public int? ConsentLifetime { get; set; }
    /// <summary>Gets or sets a value indicating whether to allow offline access.</summary>
    public bool AllowOfflineAccess { get; set; }
    /// <summary>The maximum duration (in seconds) since the last time the user authenticated.</summary>
    public int? UserSsoLifetime { get; set; }
    /// <summary>Specifies logout URI at client for HTTP front-channel based logout.</summary>
    public string FrontChannelLogoutUri { get; set; }
    /// <summary>Gets or sets a salt value used in pair-wise subjectId generation for users of this client.</summary>
    public string PairWiseSubjectSalt { get; set; }
    /// <summary>Gets or sets a value indicating whether the access token (and its claims) should be updated on a refresh token request.</summary>
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }
    /// <summary>Specifies logout URI at client for HTTP back-channel based logout.</summary>
    public string BackChannelLogoutUri { get; set; }
    /// <summary>Specifies is the user's session id should be sent to the BackChannelLogoutUri.</summary>
    public bool BackChannelLogoutSessionRequired { get; set; }
    /// <summary>Specifies whether the access token is a reference token or a self contained JWT token.</summary>
    public AccessTokenType? AccessTokenType { get; set; }
    /// <summary>
    /// Absolute: the refresh token will expire on a fixed point in time (specified by the AbsoluteRefreshTokenLifetime) 
    /// Sliding: when refreshing the token, the lifetime of the refresh token will be renewed (by the amount specified in SlidingRefreshTokenLifetime).
    /// The lifetime will not exceed AbsoluteRefreshTokenLifetime.
    /// </summary>
    public TokenExpiration RefreshTokenExpiration { get; set; }
    /// <summary>
    /// ReUse: the refresh token handle will stay the same when refreshing tokens. 
    /// OneTime: the refresh token handle will be updated when refreshing tokens.
    /// </summary>
    public TokenUsage RefreshTokenUsage { get; set; }
    /// <summary>Specifies is the user's session id should be sent to the FrontChannelLogoutUri.</summary>
    public bool FrontChannelLogoutSessionRequired { get; set; }
    /// <summary>Gets or sets a value indicating whether JWT access tokens should include an identifier.</summary>
    public bool IncludeJwtId { get; set; }
    /// <summary>Controls whether access tokens are transmitted via the browser for this client. This can prevent accidental leakage of access tokens when multiple response types are allowed.</summary>
    public bool AllowAccessTokensViaBrowser { get; set; }
    /// <summary>When requesting both an id token and access token, should the user claims always be added to the id token instead of requring the client to use the userinfo endpoint.</summary>
    public bool AlwaysIncludeUserClaimsInIdToken { get; set; }
    /// <summary>Gets or sets a value indicating whether client claims should be always included in the access tokens - or only for client credentials flow.</summary>
    public bool AlwaysSendClientClaims { get; set; }
    /// <summary>Lifetime of authorization code in seconds.</summary>
    public int AuthorizationCodeLifetime { get; set; }
    /// <summary>Specifies whether a proof key is required for authorization code based token requests.</summary>
    public bool RequirePkce { get; set; }
    /// <summary>Specifies whether a proof key can be sent using plain method.</summary>
    public bool AllowPlainTextPkce { get; set; }
    /// <summary>Gets or sets a value to prefix it on client claim types.</summary>
    public string ClientClaimsPrefix { get; set; }
    /// <summary>Specifies whether consent screen is remembered after having been given.</summary>
    public bool AllowRememberConsent { get; set; }
    /// <summary>Gets or sets the type of the device flow user code.</summary>
    public string UserCodeType { get; set; }
    /// <summary>Gets or sets the device code lifetime.</summary>
    public int DeviceCodeLifetime { get; set; }
    /// <summary>Specifies if client is enabled.</summary>
    public bool Enabled { get; set; }
    /// <summary>Sliding lifetime of a refresh token in seconds. Defaults to 1296000 seconds / 15 days.</summary>
    public int SlidingRefreshTokenLifetime { get; set; }
    /// <summary>Determines whether login using a local account is allowed for this client. </summary>
    public bool? EnableLocalLogin { get; set; }
    /// <summary>List of identity providers that are not allowed for this client.</summary>
    public IEnumerable<string> IdentityProviderRestrictions { get; set; } = new List<string>();
}

/// <summary>Models a client request.</summary>
public class BaseClientRequest
{
    /// <summary>Application name that will be seen on consent screens.</summary>
    public string ClientName { get; set; }
    /// <summary>Application URL that will be seen on consent screens.</summary>
    public string ClientUri { get; set; }
    /// <summary>Application logo that will be seen on consent screens.</summary>
    public string LogoUri { get; set; }
    /// <summary>Application description.</summary>
    public string Description { get; set; }
    /// <summary>Specifies whether a consent screen is required.</summary>
    public bool RequireConsent { get; set; }
    /// <summary>Translations.</summary>
    public TranslationDictionary<ClientTranslation> Translations { get; set; } = new();
}

/// <summary>Defines the model required to update client URLs.</summary>
public class UpdateClientUrls
{
    /// <summary>Cors origins allowed.</summary>
    public IEnumerable<string> AllowedCorsOrigins { get; set; }
    /// <summary>Allowed URIs to redirect after logout.</summary>
    public IEnumerable<string> PostLogoutRedirectUris { get; set; }
    /// <summary>Allowed URIs to redirect after successful login.</summary>
    public IEnumerable<string> RedirectUris { get; set; }
}

/// <summary>Models an OAuth client type.</summary>
public enum ClientType
{
    /// <summary>Single page application supporting authorization code.</summary>
    SPA,
    /// <summary>Classic web application.</summary>
    WebApp,
    /// <summary>A desktop or mobile application running on a user's device.</summary>
    Native,
    /// <summary>A server to server application.</summary>
    Machine,
    /// <summary>IoT application or otherwise browserless or input constrained device.</summary>
    Device,
    /// <summary>Single page application supporting implicit flow.</summary>
    SPALegacy
}
