using IdentityServer4.Models;

namespace Indice.Features.Identity.Core.Events.Models;

/// <summary>Client information about an event that occurred.</summary>
public class ClientEventContext
{
    private static readonly char[] comma = [','];

    /// <summary>Specifies if client is enabled.</summary>
    public bool Enabled { get; private set; }
    /// <summary>Unique ID of the client.</summary>
    public string ClientId { get; private set; } = null!;
    /// <summary>Gets or sets the protocol type.</summary>
    public string? ProtocolType { get; private set; }
    /// <summary>If set to false, no client secret is needed to request tokens at the token endpoint.</summary>
    public bool RequireClientSecret { get; private set; }
    /// <summary>Client display name (used for logging and consent screen).</summary>
    public string? ClientName { get; private set; }
    /// <summary>Description of the client.</summary>
    public string? Description { get; private set; }
    /// <summary>URI to further information about client (used on consent screen).</summary>
    public string? ClientUri { get; private set; }
    /// <summary>URI to client logo (used on consent screen).</summary>
    public string? LogoUri { get; private set; }
    /// <summary>Specifies whether a consent screen is required.</summary>
    public bool RequireConsent { get; private set; }
    /// <summary>Specifies whether user can choose to store consent decisions.</summary>
    public bool AllowRememberConsent { get; private set; }
    /// <summary>Specifies the allowed grant types (legal combinations of AuthorizationCode, Implicit, Hybrid, ResourceOwner, ClientCredentials).</summary>
    public List<string> AllowedGrantTypes { get; private set; } = [];
    /// <summary>Specifies whether a proof key is required for authorization code based token requests.</summary>
    public bool RequirePkce { get; private set; }
    /// <summary>Specifies whether a proof key can be sent using plain method.</summary>
    public bool AllowPlainTextPkce { get; private set; }
    /// <summary>Specifies whether the client must use a request object on authorize requests.</summary>
    public bool RequireRequestObject { get; private set; }
    /// <summary>Controls whether access tokens are transmitted via the browser for this client. This can prevent accidental leakage of access tokens when multiple response types are allowed.</summary>
    public bool AllowAccessTokensViaBrowser { get; private set; }
    /// <summary>Specifies allowed URIs to return tokens or authorization codes to.</summary>
    public List<string> RedirectUris { get; private set; } = [];
    /// <summary>Specifies allowed URIs to redirect to after logout.</summary>
    public List<string> PostLogoutRedirectUris { get; private set; } = [];
    /// <summary>Specifies logout URI at client for HTTP front-channel based logout.</summary>
    public string? FrontChannelLogoutUri { get; private set; }
    /// <summary>Specifies if the user's session id should be sent to the FrontChannelLogoutUri.</summary>
    public bool FrontChannelLogoutSessionRequired { get; private set; }
    /// <summary>Specifies logout URI at client for HTTP back-channel based logout.</summary>
    public string? BackChannelLogoutUri { get; private set; }
    /// <summary>Specifies if the user's session id should be sent to the BackChannelLogoutUri.</summary>
    public bool BackChannelLogoutSessionRequired { get; private set; }
    /// <summary>Gets or sets a value indicating whether [allow offline access].</summary>
    public bool AllowOfflineAccess { get; private set; }
    /// <summary>Specifies the api scopes that the client is allowed to request. If empty, the client can't access any scope.</summary>
    public List<string> AllowedScopes { get; private set; } = [];
    /// <summary>When requesting both an id token and access token, should the user claims always be added to the id token instead of requiring the client to use the user info endpoint.</summary>
    public bool AlwaysIncludeUserClaimsInIdToken { get; private set; }
    /// <summary>Lifetime of identity token in seconds (defaults to 300 seconds / 5 minutes).</summary>
    public int IdentityTokenLifetime { get; private set; }
    /// <summary>Signing algorithm for identity token. If empty, will use the server default signing algorithm.</summary>
    public List<string> AllowedIdentityTokenSigningAlgorithms { get; private set; } = [];
    /// <summary>Lifetime of access token in seconds (defaults to 3600 seconds / 1 hour).</summary>
    public int AccessTokenLifetime { get; private set; }
    /// <summary>Lifetime of authorization code in seconds (defaults to 300 seconds / 5 minutes).</summary>
    public int AuthorizationCodeLifetime { get; private set; }
    /// <summary>Maximum lifetime of a refresh token in seconds. Defaults to 2592000 seconds / 30 days.</summary>
    public int AbsoluteRefreshTokenLifetime { get; private set; }
    /// <summary>Sliding lifetime of a refresh token in seconds. Defaults to 1296000 seconds / 15 days.</summary>
    public int SlidingRefreshTokenLifetime { get; private set; }
    /// <summary>Lifetime of a user consent in seconds. Defaults to null (no expiration).</summary>
    public int? ConsentLifetime { get; private set; }
    /// <summary>
    /// ReUse: the refresh token handle will stay the same when refreshing tokens
    /// OneTime: the refresh token handle will be updated when refreshing tokens
    /// </summary>
    public TokenUsage RefreshTokenUsage { get; private set; }
    /// <summary>Gets or sets a value indicating whether the access token (and its claims) should be updated on a refresh token request.</summary>
    public bool UpdateAccessTokenClaimsOnRefresh { get; private set; }
    /// <summary>
    /// Absolute: the refresh token will expire on a fixed point in time (specified by the AbsoluteRefreshTokenLifetime)
    /// Sliding: when refreshing the token, the lifetime of the refresh token will be renewed (by the amount specified in SlidingRefreshTokenLifetime). The lifetime will not exceed AbsoluteRefreshTokenLifetime.
    /// </summary>        
    public TokenExpiration RefreshTokenExpiration { get; private set; }
    /// <summary>Specifies whether the access token is a reference token or a self contained JWT token (defaults to Jwt).</summary>
    public AccessTokenType AccessTokenType { get; private set; }
    /// <summary>Gets or sets a value indicating whether the local login is allowed for this client.</summary>
    public bool EnableLocalLogin { get; private set; }
    /// <summary>Specifies which external IdPs can be used with this client (if list is empty all IdPs are allowed).</summary>
    public List<string> IdentityProviderRestrictions { get; private set; } = [];
    /// <summary>Gets or sets a value indicating whether JWT access tokens should include an identifier.</summary>
    public bool IncludeJwtId { get; private set; }
    /// <summary>Allows settings claims for the client (will be included in the access token).</summary>
    public List<ClientClaimEventContext> Claims { get; private set; } = [];
    /// <summary>Gets or sets a value indicating whether client claims should be always included in the access tokens - or only for client credentials flow.</summary>
    public bool AlwaysSendClientClaims { get; private set; }
    /// <summary>Gets or sets a value to prefix it on client claim types.</summary>
    public string? ClientClaimsPrefix { get; private set; }
    /// <summary>Gets or sets a salt value used in pair-wise subjectId generation for users of this client.</summary>
    public string? PairWiseSubjectSalt { get; private set; }
    /// <summary>The maximum duration (in seconds) since the last time the user authenticated.</summary>
    public int? UserSsoLifetime { get; private set; }
    /// <summary>Gets or sets the type of the device flow user code.</summary>
    public string? UserCodeType { get; private set; }
    /// <summary>Gets or sets the device code lifetime.</summary>
    public int DeviceCodeLifetime { get; private set; }
    /// <summary>Gets or sets the allowed CORS origins for JavaScript clients.</summary>
    public List<string> AllowedCorsOrigins { get; private set; } = [];
    /// <summary>Gets or sets the custom properties for the client.</summary>
    public IDictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

    /// <summary>Creates a new <see cref="ClientEventContext"/> instance given a <see cref="Client"/> entity.</summary>
    /// <param name="client">The client entity.</param>
    public static ClientEventContext InitializeFromClient(IdentityServer4.EntityFramework.Entities.Client client) => new() {
        AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
        AccessTokenLifetime = client.AccessTokenLifetime,
        AccessTokenType = (AccessTokenType)client.AccessTokenType,
        AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser,
        AllowedCorsOrigins = client.AllowedCorsOrigins?.Select(origin => origin.Origin)?.ToList() ?? [],
        AllowedGrantTypes = client.AllowedGrantTypes?.Select(grant => grant.GrantType)?.ToList() ?? [],
        AllowedIdentityTokenSigningAlgorithms = client.AllowedIdentityTokenSigningAlgorithms?.Split(comma, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList() ?? [],
        AllowedScopes = client.AllowedScopes?.Select(scope => scope.Scope)?.ToList() ?? [],
        AllowOfflineAccess = client.AllowOfflineAccess,
        AllowPlainTextPkce = client.AllowPlainTextPkce,
        AllowRememberConsent = client.AllowRememberConsent,
        AlwaysIncludeUserClaimsInIdToken = client.AlwaysIncludeUserClaimsInIdToken,
        AlwaysSendClientClaims = client.AlwaysSendClientClaims,
        AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
        BackChannelLogoutSessionRequired = client.BackChannelLogoutSessionRequired,
        BackChannelLogoutUri = client.BackChannelLogoutUri,
        Claims = client.Claims?.Select(claim => new ClientClaimEventContext(claim.Type, claim.Value)).ToList() ?? [],
        ClientClaimsPrefix = client.ClientClaimsPrefix,
        ClientId = client.ClientId,
        ClientName = client.ClientName,
        ClientUri = client.ClientUri,
        ConsentLifetime = client.ConsentLifetime,
        Description = client.Description,
        DeviceCodeLifetime = client.DeviceCodeLifetime,
        Enabled = client.Enabled,
        EnableLocalLogin = client.EnableLocalLogin,
        FrontChannelLogoutSessionRequired = client.FrontChannelLogoutSessionRequired,
        FrontChannelLogoutUri = client.FrontChannelLogoutUri,
        IdentityProviderRestrictions = client.IdentityProviderRestrictions?.Select(restriction => restriction.Provider).ToList() ?? [],
        IdentityTokenLifetime = client.IdentityTokenLifetime,
        IncludeJwtId = client.IncludeJwtId,
        LogoUri = client.LogoUri,
        PairWiseSubjectSalt = client.PairWiseSubjectSalt,
        PostLogoutRedirectUris = client.PostLogoutRedirectUris?.Select(uri => uri.PostLogoutRedirectUri)?.ToList() ?? [],
        Properties = client.Properties?.ToDictionary(property => property.Key, property => property.Value) ?? [],
        ProtocolType = client.ProtocolType,
        RedirectUris = client.RedirectUris?.Select(uri => uri.RedirectUri)?.ToList() ?? [],
        RefreshTokenExpiration = (TokenExpiration)client.RefreshTokenExpiration,
        RefreshTokenUsage = (TokenUsage)client.RefreshTokenUsage,
        RequireClientSecret = client.RequireClientSecret,
        RequireConsent = client.RequireConsent,
        RequirePkce = client.RequirePkce,
        RequireRequestObject = client.RequireRequestObject,
        SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
        UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
        UserCodeType = client.UserCodeType,
        UserSsoLifetime = client.UserSsoLifetime
    };
}

/// <summary>Client claim.</summary>
/// <remarks>Creates a new instance of <see cref="ClientClaimEventContext"/>.</remarks>
/// <param name="type">The claim type for this claim.</param>
/// <param name="value">The claim value for this claim.</param>
public class ClientClaimEventContext(string type, string value)
{
    /// <summary>Gets or sets the claim type for this claim.</summary>
    public string Type { get; } = type;
    /// <summary>Gets or sets the claim value for this claim.</summary>
    public string Value { get; } = value;
}
