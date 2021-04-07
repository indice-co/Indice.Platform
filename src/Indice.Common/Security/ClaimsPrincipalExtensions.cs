using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;

namespace Indice.Security
{
    /// <summary>
    /// Extension methods on <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Finds a display name for the user based on <see cref="BasicClaimTypes.GivenName"/>, <see cref="BasicClaimTypes.FamilyName"/> and <see cref="BasicClaimTypes.Email"/> claims.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        public static string FindDisplayName(this ClaimsPrincipal principal) {
            var displayName = default(string);
            var name = principal.FindFirst(BasicClaimTypes.Name)?.Value;
            var firstName = principal.FindFirst(BasicClaimTypes.GivenName)?.Value;
            var lastName = principal.FindFirst(BasicClaimTypes.FamilyName)?.Value;
            var email = principal.FindFirst(BasicClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName)) {
                displayName = $"{firstName} {lastName}".Trim();
            } else if (!string.IsNullOrEmpty(name)) {
                displayName = name;
            } else if (!string.IsNullOrEmpty(email)) {
                displayName = email;
            }
            return displayName;
        }

        /// <summary>
        /// Gets the user's unique id.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        public static string FindSubjectId(this ClaimsPrincipal principal) => principal.FindFirst(BasicClaimTypes.Subject)?.Value;

        private static bool TryFindFirstValue<T>(this ClaimsPrincipal principal, string claimType, out T result) where T : struct {
            result = default;
            var valueString = principal.FindFirst(c => c.Type == claimType)?.Value;
            object value = default(T);
            if (valueString == null) {
                result = (T)value;
                return false;
            }
            var type = typeof(T);
            if (type.GetTypeInfo().IsEnum) {
                value = Enum.Parse(type, valueString, true);
            } else if (type == typeof(bool)) {
                value = bool.Parse(valueString);
            } else if (type == typeof(int)) {
                value = int.Parse(valueString);
            } else if (type == typeof(Guid)) {
                value = Guid.Parse(valueString);
            } else if (type == typeof(double)) {
                value = double.Parse(valueString, CultureInfo.InvariantCulture);
            } else if (type == typeof(DateTime)) {
                value = DateTime.Parse(valueString, CultureInfo.InvariantCulture);
            } else if (type == typeof(TimeSpan)) {
                value = TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);
            }
            result = (T)value;
            return true;
        }

        /// <summary>
        /// Finds the value of the specified claim.
        /// </summary>
        /// <typeparam name="T">The type of the claim's value.</typeparam>
        /// <param name="principal">The current principal.</param>
        /// <param name="claimType">The claim type.</param>
        public static T? FindFirstValue<T>(this ClaimsPrincipal principal, string claimType) where T : struct {
            if (TryFindFirstValue(principal, claimType, out T value)) {
                return value;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Checks if the current principal is a client owned by the system.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        /// <returns></returns>
        public static bool IsSystemClient(this ClaimsPrincipal principal) {
            var isSystem = FindFirstValue<bool>(principal, $"client_{BasicClaimTypes.System}") ?? FindFirstValue<bool>(principal, BasicClaimTypes.System);
            return isSystem ?? false;
        }

        /// <summary>
        /// Checks if the current principal is a system admin.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        public static bool IsAdmin(this ClaimsPrincipal principal) => FindFirstValue<bool>(principal, BasicClaimTypes.Admin) ?? principal.HasClaim("role", "Administrator");

        /// <summary>
        /// Checks if the current principal has logged in using an external provider.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        public static bool IsExternal(this ClaimsPrincipal principal) => principal.FindFirst("idp")?.Value != "local";

        /// <summary>
        /// Logic for normalizing scope claims to separate claim types.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        /// <param name="separator">The character that separates scopes.</param>
        public static ClaimsPrincipal NormalizeScopeClaims(this ClaimsPrincipal principal, char separator = ' ') {
            var identities = new List<ClaimsIdentity>();
            foreach (var id in principal.Identities) {
                var identity = new ClaimsIdentity(id.AuthenticationType, id.NameClaimType, id.RoleClaimType);
                foreach (var claim in id.Claims) {
                    if (claim.Type == "scope") {
                        if (claim.Value.Contains(" ")) {
                            var scopes = claim.Value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var scope in scopes) {
                                identity.AddClaim(new Claim("scope", scope, claim.ValueType, claim.Issuer));
                            }
                        } else {
                            identity.AddClaim(claim);
                        }
                    } else {
                        identity.AddClaim(claim);
                    }
                }
                identities.Add(identity);
            }
            return new ClaimsPrincipal(identities);
        }
    }
}
