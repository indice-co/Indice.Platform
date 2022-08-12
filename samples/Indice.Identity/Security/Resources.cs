using System.Collections.Generic;
using System.Linq;
using IdentityModel;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.Security;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Contains methods to get the system's predefined identity and API resources.
    /// </summary>
    public static class Resources
    {
        private static readonly IEnumerable<string> _userClaims = new[] {
            BasicClaimTypes.Admin,
            BasicClaimTypes.PasswordExpirationDate,
            BasicClaimTypes.PasswordExpirationPolicy,
            BasicClaimTypes.System,
            BasicClaimTypes.OtpAuthenticated,
            JwtClaimTypes.Email,
            JwtClaimTypes.EmailVerified,
            JwtClaimTypes.FamilyName,
            JwtClaimTypes.GivenName,
            JwtClaimTypes.Name,
            JwtClaimTypes.PhoneNumber,
            JwtClaimTypes.PhoneNumberVerified,
            JwtClaimTypes.Role,
            JwtClaimTypes.Subject,
            BasicClaimTypes.MsGraphToken
        };

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
                Emphasize = true,
                UserClaims = new IdentityResources.Profile().UserClaims.Concat(new [] {
                    BasicClaimTypes.PasswordExpirationDate,
                    BasicClaimTypes.PasswordExpirationPolicy
                })
                .ToList()
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
            },
            new IdentityResources.Phone {
                Description = "Your phone number.",
                DisplayName = nameof(IdentityResources.Phone),
                Name = nameof(IdentityResources.Phone).ToLower(),
                Required = true,
                UserClaims = new IdentityResources.Phone().UserClaims.Concat(new [] {
                    BasicClaimTypes.OtpAuthenticated
                })
                .ToList()
            },
            new IdentityResource {
                Name = BasicClaimTypes.MsGraphToken,
                DisplayName = "MS Graph access token",
                Description = "Your access token when logging in with your Microsoft account.",
                Required = true,
                UserClaims = {
                    BasicClaimTypes.MsGraphToken
                }
            }
        };

        public static IEnumerable<ApiScope> GetApiScopes() => new[] {
            new ApiScope(IdentityServerApi.Scope, "IdentityServer API", _userClaims) {
                Description  = "API backing the IdentityServer Management Tool."
            },
            new ApiScope(IdentityServerApi.SubScopes.Clients, "IdentityServer Clients API", _userClaims) {
                Description = "Provides access to the clients management API."
            },
            new ApiScope(IdentityServerApi.SubScopes.Users, "IdentityServer Users API", _userClaims) {
                Description = "Provides access to the users management API."
            },
            new ApiScope("backoffice", "Backoffice API", _userClaims) {
                Description = "Provides access to the backoffice operations."
            },
            new ApiScope("backoffice:messages", "Messages API", _userClaims) {
                Description = "Provides access to campaign management operations."
            }
        };

        /// <summary>
        /// Gets the system's predefined APIs.
        /// </summary>
        public static IEnumerable<ApiResource> GetApis() {
            var identityApi = new ApiResource(IdentityServerApi.Scope, "IdentityServer API", _userClaims) {
                ApiSecrets = {
                    new Secret("VGLwBUKNQbfZABgZgD45PshqPZHkYJVrFPKR4QKsZRLdzAnzU2UHzQUHc2Zhd759".ToSha256())
                },
                Description = "APIs backing the IdentityServer management tool.",
                Scopes = { IdentityServerApi.Scope, IdentityServerApi.SubScopes.Clients, IdentityServerApi.SubScopes.Users }
            };
            var backofficeApi = new ApiResource("backoffice", "Backoffice API", _userClaims) {
                ApiSecrets = {
                    new Secret("exTyrC9cVADKp5T8g24hEKVjpUsf9S8KQ7Zz4q7grPbd6JvNKzaZxPwzNbpcfHVP".ToSha256())
                },
                Description = "APIs backing the backoffice tool.",
                Scopes = { "backoffice", "backoffice:messages" }
            };
            return new[] { identityApi, backofficeApi };
        }
    }
}
