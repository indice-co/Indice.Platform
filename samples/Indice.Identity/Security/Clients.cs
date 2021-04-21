using System.Collections.Generic;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.Api.Security;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Contains helper methods to work with system clients.
    /// </summary>
    public static class Clients
    {
        /// <summary>
        /// Get system's predefined clients.
        /// </summary>
        public static IEnumerable<Client> Get() => new List<Client> {
            new Client {
                ClientId = "postman",
                ClientName = "PostMan",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                AllowedScopes = {
                    IdentityServerApi.Scope,
                    IdentityServerApi.SubScopes.Clients,
                    IdentityServerApi.SubScopes.Users,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    JwtClaimTypes.Role
                },
                AllowOfflineAccess = false,
                ClientSecrets = {
                    new Secret("qmHK5Rqc4rZTBE3BqCw6ScxR5N57rnfzcULdmKA33cc8RjhsZsqUygMVX43y6yJB".ToSha256())
                },
                PostLogoutRedirectUris = {
                    "https://www.getpostman.com"
                },
                RedirectUris = {
                    "https://www.getpostman.com/oauth2/callback"
                },
                RequireConsent = false
            },
            new Client {
                ClientId = "resource-owner-password-mvc",
                ClientName = "Resource Owner Password MVC client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AccessTokenType = AccessTokenType.Reference,
                AccessTokenLifetime = 300,
                ClientSecrets = {
                    new Secret("ZWU0NTdmNWEtM2Y0MC00NzhiLWE1ZmUtZDFhZjA4YjlmMmEy".ToSha256())
                },
                AllowedScopes = {
                    IdentityServerApi.Scope,
                    IdentityServerApi.SubScopes.Clients,
                    IdentityServerApi.SubScopes.Users,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    JwtClaimTypes.Role
                },
                AllowOfflineAccess = true,
                RequireClientSecret = true,
                RequireConsent = false
            },
            new Client {
                ClientId = "idsrv-admin-ui",
                ClientName = "IdentityServer Management Tool",
                AccessTokenType = AccessTokenType.Reference,
                AllowAccessTokensViaBrowser = false,
                AllowedCorsOrigins = {
                    "http://localhost:4200",
                    "https://indice-idsrv.azurewebsites.net",
                    "https://localhost:2000"
                },
                AllowedGrantTypes = GrantTypes.Code,
                AllowedScopes = {
                    IdentityServerApi.Scope,
                    IdentityServerApi.SubScopes.Clients,
                    IdentityServerApi.SubScopes.Users,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    JwtClaimTypes.Role
                },
                AllowOfflineAccess = true,
                ClientUri = "https://indice-idsrv.azurewebsites.net/admin",
                PostLogoutRedirectUris = {
                    "http://localhost:4200/admin/logged-out",
                    "https://localhost:2000/admin/logged-out"
                },
                RedirectUris = {
                    "http://localhost:4200/admin/auth-callback",
                    "http://localhost:4200/admin/auth-renew",
                    "https://localhost:2000/admin/auth-callback",
                    "https://localhost:2000/admin/auth-renew"
                },
                RequireClientSecret  = false,
                RequirePkce = true,
                RequireConsent = true
            },
            new Client {
                ClientId = "code-flow-iframe",
                ClientName = "Code Flow iframe",
                AccessTokenType = AccessTokenType.Reference,
                AllowAccessTokensViaBrowser = false,
                AllowedCorsOrigins = {
                    "https://localhost:2002"
                },
                AllowedGrantTypes = GrantTypes.Hybrid,
                AllowedScopes = {
                    IdentityServerApi.Scope,
                    IdentityServerApi.SubScopes.Clients,
                    IdentityServerApi.SubScopes.Users,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    JwtClaimTypes.Role
                },
                AllowOfflineAccess = true,
                ClientUri = "https://localhost:2002",
                PostLogoutRedirectUris = {
                    "https://localhost:2002/account/logged-out"
                },
                RedirectUris = {
                    "https://localhost:2002/account/auth-callback"
                },
                RequireClientSecret  = false,
                RequirePkce = true,
                RequireConsent = false
            },
            new Client {
                ClientId = "swagger-ui",
                ClientName = "Swagger UI",
                AccessTokenType = AccessTokenType.Reference,
                AllowAccessTokensViaBrowser = false,
                AllowedCorsOrigins = {
                    "https://localhost:2000",
                    "https://idsrv-admin-ui.azurewebsites.net"
                },
                AllowedGrantTypes = GrantTypes.Code,
                AllowedScopes = {
                    IdentityServerApi.Scope,
                    IdentityServerApi.SubScopes.Clients,
                    IdentityServerApi.SubScopes.Users,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    JwtClaimTypes.Role
                },
                PostLogoutRedirectUris = {
                    "https://localhost:2000/docs",
                    "https://idsrv-admin-ui.azurewebsites.net/docs"
                },
                RedirectUris = {
                    "https://localhost:2000/docs/oauth2-redirect.html",
                    "https://idsrv-admin-ui.azurewebsites.net/docs/oauth2-redirect.html"
                },
                RequireConsent = true,
                RequirePkce = true,
                RequireClientSecret = false
            },
            new Client {
                ClientId = "ppk-client",
                ClientName = "Public/Private key client",
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessTokensViaBrowser = false,
                AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                ClientSecrets = { 
                    new Secret("JUEKX2XugFv5XrX3".ToSha256())
                },
                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Phone
                },
                RequireConsent = false,
                RequirePkce = false,
                RequireClientSecret = true,
                AllowOfflineAccess = true,
                AlwaysSendClientClaims = true,
                Claims = {
                    new ClientClaim(BasicClaimTypes.TrustedDevice, bool.TrueString.ToLower())
                }
            }
        };
    }
}
