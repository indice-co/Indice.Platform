using System.Collections.Generic;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.Features;

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
                    "https://idsrvadminui.z1.web.core.windows.net",
                    "https://indice-admin-ui.azureedge.net"
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
                ClientUri = "https://indice-admin-ui.azureedge.net",
                PostLogoutRedirectUris = {
                    "http://localhost:4200/#/logged-out",
                    "https://idsrvadminui.z1.web.core.windows.net/#/logged-out",
                    "https://indice-admin-ui.azureedge.net/#/logged-out"
                },
                RedirectUris = {
                    "http://localhost:4200/#/auth-callback",
                    "https://idsrvadminui.z1.web.core.windows.net/#/auth-callback",
                    "https://indice-admin-ui.azureedge.net/#/auth-callback",
                    "http://localhost:4200/#/auth-renew",
                    "https://idsrvadminui.z1.web.core.windows.net/#/auth-renew",
                    "https://indice-admin-ui.azureedge.net/#/auth-renew"
                },
                RequireClientSecret  = false,
                RequirePkce = true,
                RequireConsent = false
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
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessTokensViaBrowser = false,
                AllowedCorsOrigins = {
                    "https://localhost:2000",
                    "https://idsrv-admin-ui.azurewebsites.net"
                },
                ClientSecrets = {
                    new Secret("M2YwNTlkMTgtYWQzNy00MGNjLWFiYjQtZWQ3Y2Y4N2M3YWU3".ToSha256())
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
            }
        };
    }
}
