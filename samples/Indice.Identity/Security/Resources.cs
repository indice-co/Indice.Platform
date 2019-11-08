using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.Features;
using Indice.Security;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Contains methods to get the system's predefined identity and API resources.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Gets the system's predefined identity resources.
        /// </summary>
        public static IEnumerable<IdentityResource> GetIdentityResources() => new[] {
            new IdentityResources.OpenId {
                Description = "Your user identifier.",
                DisplayName = nameof(IdentityResources.OpenId),
                Name = nameof(IdentityResources.OpenId).ToLower(),
                Required = true
            },
            new IdentityResources.Profile {
                Description = "Your user profile information (first name, last name, etc.)",
                DisplayName = nameof(IdentityResources.Profile),
                Name = nameof(IdentityResources.Profile).ToLower(),
                Emphasize = true
            },
            new IdentityResource {
                Description = "Your user role on the system.",
                DisplayName = nameof(JwtClaimTypes.Role),
                Name = JwtClaimTypes.Role,
                Required = true,
                UserClaims = {
                    JwtClaimTypes.Role,
                    BasicClaimTypes.Admin,
                    BasicClaimTypes.System
                }
            },
            new IdentityResources.Email {
                Description = "Your user email address.",
                DisplayName = nameof(IdentityResources.Email),
                Name = nameof(IdentityResources.Email).ToLower(),
                Required = true
            }
        };

        /// <summary>
        /// Gets the system's predefined API resources.
        /// </summary>
        public static IEnumerable<ApiResource> GetApiResources() {
            var claimTypes = new[] {
                JwtClaimTypes.Role,
                BasicClaimTypes.Admin,
                BasicClaimTypes.System,
                JwtClaimTypes.Subject,
                JwtClaimTypes.Name,
                JwtClaimTypes.Email,
                JwtClaimTypes.GivenName,
                JwtClaimTypes.FamilyName
            };
            var identityApi = new ApiResource(IdentityServerApi.Scope, "IdentityServer API", claimTypes) {
                ApiSecrets = {
                    new Secret("VGLwBUKNQbfZABgZgD45PshqPZHkYJVrFPKR4QKsZRLdzAnzU2UHzQUHc2Zhd759".ToSha256())
                },
                Description = "API backing the IdentityServer Management Tool."
            };
            identityApi.Scopes.Add(new Scope {
                Description = "Provides access to the clients management API.",
                DisplayName = "IdentityServer Clients API",
                Name = IdentityServerApi.SubScopes.Clients,
                Required = true
            });
            identityApi.Scopes.Add(new Scope {
                Description = "Provides access to the users management API.",
                DisplayName = "IdentityServer Users API",
                Name = IdentityServerApi.SubScopes.Users,
                Required = true
            });
            return new[] {
                identityApi
            };
        }
    }
}
