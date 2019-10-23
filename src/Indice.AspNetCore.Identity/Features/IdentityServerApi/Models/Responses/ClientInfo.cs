using System.Collections.Generic;
using IdentityServer4.Models;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a system client.
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// The unique identifier for this application.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Application name that will be seen on consent screens.
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// Application description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Determines whether this application is enabled or not.
        /// </summary>
        public bool? Enabled { get; set; }
        /// <summary>
        /// Specifies whether a consent screen is required.
        /// </summary>
        public bool? RequireConsent { get; set; }
        /// <summary>
        /// Specifies whether consent screen is remembered after having been given.
        /// </summary>
        public bool? AllowRememberConsent { get; set; }
        /// <summary>
        /// Application logo that will be seen on consent screens.
        /// </summary>
        public string LogoUri { get; set; }
        /// <summary>
        /// Application URL that will be seen on consent screens.
        /// </summary>
        public string ClientUri { get; set; }
        /// <summary>
        /// Specifies whether the client can be edited or not.
        /// </summary>
        public bool NonEditable { get; set; }
    }

    /// <summary>
    /// Models a system client when API provides info for a single client.
    /// </summary>
    public class SingleClientInfo : ClientInfo
    {
        /// <summary>
        /// Cors origins allowed.
        /// </summary>
        public IEnumerable<string> AllowedCorsOrigins { get; set; }
        /// <summary>
        /// Allowed URIs to redirect after logout.
        /// </summary>
        public IEnumerable<string> PostLogoutRedirectUris { get; set; }
        /// <summary>
        /// Allowed URIs to redirect after successful login.
        /// </summary>
        public IEnumerable<string> RedirectUris { get; set; }
        /// <summary>
        /// The API resources that the client has access to.
        /// </summary>
        public IEnumerable<string> ApiResources { get; set; }
        /// <summary>
        /// The identity resources that the client has access to.
        /// </summary>
        public IEnumerable<string> IdentityResources { get; set; }
        /// <summary>
        /// Lifetime of identity token in seconds.
        /// </summary>
        public int? IdentityTokenLifetime { get; set; }
        /// <summary>
        /// Lifetime of access token in seconds
        /// </summary>
        public int? AccessTokenLifetime { get; set; }
        /// <summary>
        /// Lifetime of a user consent in seconds.
        /// </summary>
        public int? ConsentLifetime { get; set; }
        /// <summary>
        /// The maximum duration (in seconds) since the last time the user authenticated.
        /// </summary>
        public int? UserSsoLifetime { get; set; }
        /// <summary>
        /// Specifies logout URI at client for HTTP front-channel based logout.
        /// </summary>
        public string FrontChannelLogoutUri { get; set; }
        /// <summary>
        /// Gets or sets a salt value used in pair-wise subjectId generation for users of this client.
        /// </summary>
        public string PairWiseSubjectSalt { get; set; }
        /// <summary>
        /// Specifies whether the access token is a reference token or a self contained JWT token.
        /// </summary>
        public AccessTokenType? AccessTokenType { get; set; }
        /// <summary>
        /// Specifies is the user's session id should be sent to the FrontChannelLogoutUri.
        /// </summary>
        public bool? FrontChannelLogoutSessionRequired { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether JWT access tokens should include an identifier.
        /// </summary>
        public bool? IncludeJwtId { get; set; }
        /// <summary>
        /// Controls whether access tokens are transmitted via the browser for this client. This can prevent accidental leakage of access tokens when multiple response types are allowed.
        /// </summary>
        public bool? AllowAccessTokensViaBrowser { get; set; }
        /// <summary>
        /// When requesting both an id token and access token, should the user claims always be added to the id token instead of requring the client to use the userinfo endpoint.
        /// </summary>
        public bool? AlwaysIncludeUserClaimsInIdToken { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether client claims should be always included in the access tokens - or only for client credentials flow.
        /// </summary>
        public bool? AlwaysSendClientClaims { get; set; }
        /// <summary>
        /// Lifetime of authorization code in seconds.
        /// </summary>
        public int? AuthorizationCodeLifetime { get; set; }
        /// <summary>
        /// Specifies whether a proof key is required for authorization code based token requests.
        /// </summary>
        public bool? RequirePkce { get; set; }
        /// <summary>
        /// Specifies whether a proof key can be sent using plain method.
        /// </summary>
        public bool? AllowPlainTextPkce { get; set; }
        /// <summary>
        /// Gets or sets a value to prefix it on client claim types.
        /// </summary>
        public string ClientClaimsPrefix { get; set; }
        /// <summary>
        /// List of client claims.
        /// </summary>
        public IEnumerable<ClaimInfo> Claims { get; set; }
        /// <summary>
        /// List of configured grant types.
        /// </summary>
        public IEnumerable<string> GrantTypes { get; set; }
        /// <summary>
        /// List of available client secrets.
        /// </summary>
        public IEnumerable<ClientSecretInfo> Secrets { get; set; }
    }
}
