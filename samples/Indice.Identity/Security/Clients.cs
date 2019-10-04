using System;
using System.Collections.Generic;
using System.Linq;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using Indice.Identity.Models;
using Entities = IdentityServer4.EntityFramework.Entities;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Contains helper methods to work with system clients.
    /// </summary>
    public class Clients
    {
        /// <summary>
        /// Get system's predefined clients.
        /// </summary>
        /// <returns></returns>
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
                    new Secret("qmHK5Rqc4rZTBE3BqCw6ScxR5N57rnfzcULdmKA33cc8RjhsZsqUygMVX43y6yJB".Sha256())
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
                ClientId = "idsrv-admin-ui",
                ClientName = "IdentityServer Management Tool",
                AccessTokenType = AccessTokenType.Reference,
                AllowAccessTokensViaBrowser = true,
                AllowedCorsOrigins = {
                    "http://localhost:4200",
                    "https://idsrv-admin-ui.azurewebsites.net"
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
                ClientUri = "https://idsrv-admin-ui.azurewebsites.net",
                PostLogoutRedirectUris = {
                    "http://localhost:4200/logged-out",
                    "https://idsrv-admin-ui.azurewebsites.net/logged-out"
                },
                RedirectUris = {
                    "http://localhost:4200/auth-callback",
                    "https://idsrv-admin-ui.azurewebsites.net/auth-callback",
                    "http://localhost:4200/auth-renew",
                    "https://idsrv-admin-ui.azurewebsites.net/auth-renew"
                },
                RequireClientSecret  = false,
                RequirePkce = true,
                RequireConsent = false
            },
            new Client {
                ClientId = "swagger-ui",
                ClientName = "Swagger UI",
                AccessTokenType = AccessTokenType.Reference,
                AllowAccessTokensViaBrowser = true,
                AllowedCorsOrigins = {
                    "https://localhost:5000",
                    "https://idsrv-admin-ui.azurewebsites.net"
                },
                AllowedGrantTypes = GrantTypes.Implicit,
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
                    "https://localhost:5000/docs",
                    "https://idsrv-admin-ui.azurewebsites.net/docs"
                },
                RedirectUris = {
                    "https://localhost:5000/docs/oauth2-redirect.html",
                    "https://idsrv-admin-ui.azurewebsites.net/docs/oauth2-redirect.html"
                },
                RequireConsent = true
            }
        };

        /// <summary>
        /// Creates default client configuration based on client type.
        /// </summary>
        /// <param name="clientType">The type of the client.</param>
        /// <param name="authorityUri">The IdentityServer instance URI.</param>
        /// <param name="clientRequest">Client information provided by the user.</param>
        public static Entities.Client CreateForType(ClientType clientType, string authorityUri, CreateClientRequest clientRequest) {
            var client = new Entities.Client {
                ClientId = clientRequest.ClientId,
                ClientName = clientRequest.ClientName,
                Description = clientRequest.Description,
                ClientUri = clientRequest.ClientUri,
                LogoUri = clientRequest.LogoUri,
                RequireConsent = clientRequest.RequireConsent,
                AllowedScopes = clientRequest.IdentityResources
                                             .Union(clientRequest.ApiResources)
                                             .Select(scope => new Entities.ClientScope {
                                                 Scope = scope
                                             })
                                             .ToList()
            };
            if (!string.IsNullOrEmpty(clientRequest.RedirectUri)) {
                client.RedirectUris = new List<Entities.ClientRedirectUri> {
                        new Entities.ClientRedirectUri {
                            RedirectUri = clientRequest.RedirectUri
                        }
                    };
            }
            if (!string.IsNullOrEmpty(clientRequest.PostLogoutRedirectUri)) {
                client.PostLogoutRedirectUris = new List<Entities.ClientPostLogoutRedirectUri> {
                    new Entities.ClientPostLogoutRedirectUri {
                        PostLogoutRedirectUri = clientRequest.PostLogoutRedirectUri
                    }
                };
            }
            if (clientRequest.Secrets.Any()) {
                client.ClientSecrets = clientRequest.Secrets.Select(x => new Entities.ClientSecret { 
                   Type = $"{x.Type}",
                   Description = x.Description,
                   Expiration = x.Expiration,
                   Value = x.Value
                }).ToList();
            }
            switch (clientType) {
                case ClientType.SPA:
                    client.AllowedGrantTypes = new List<Entities.ClientGrantType> {
                        new Entities.ClientGrantType {
                            GrantType = GrantType.AuthorizationCode
                        }
                    };
                    client.RequirePkce = true;
                    client.AllowedCorsOrigins = new List<Entities.ClientCorsOrigin> {
                        new Entities.ClientCorsOrigin {
                            Origin = clientRequest.ClientUri ?? authorityUri
                        }
                    };
                    break;
                case ClientType.WebApp:
                    client.AllowedGrantTypes = new List<Entities.ClientGrantType> {
                        new Entities.ClientGrantType {
                            GrantType = GrantType.Hybrid
                        }
                    };
                    break;
                case ClientType.Native:
                    break;
                case ClientType.Machine:
                    client.AllowedGrantTypes = new List<Entities.ClientGrantType> {
                        new Entities.ClientGrantType {
                            GrantType = GrantType.ClientCredentials
                        }
                    };
                    client.RequireConsent = false;
                    break;
                case ClientType.Device:
                    break;
                case ClientType.SPALegacy:
                    client.AllowedGrantTypes = new List<Entities.ClientGrantType> {
                        new Entities.ClientGrantType {
                            GrantType = GrantType.Implicit
                        }
                    };
                    break;
                default:
                    throw new ArgumentNullException(nameof(clientType), "Cannot determine the type of the client.");
            }
            return client;
        }
    }
}
