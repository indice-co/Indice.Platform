using System;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;

namespace Indice.AspNetCore.Security
{

    public static class ClaimsPrincipalExtensions
    {
        public static string GetDisplayName(this ClaimsPrincipal principal) {
            var displayName = default(string);
            var firstName = principal.FindFirst("given_name")?.Value;
            var lastName = principal.FindFirst("family_name")?.Value;

            if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName)) {
                displayName = $"{firstName} {lastName}".Trim();
            }

            return displayName;
        }
        
        private static bool TryFindFirstValue<T>(this ClaimsPrincipal principal, string claimType, out T result) where T : struct {
            result = default(T);
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

        public static T? FindFirstValue<T>(this ClaimsPrincipal principal, string claimType) where T : struct {
            if (TryFindFirstValue(principal, claimType, out T value))
                return value;
            else
                return null;
        }
        
        public static bool IsSystem(this ClaimsPrincipal principal) {
            var isSystem = FindFirstValue<bool>(principal, $"client_{BasicClaimTypes.System}");
            return isSystem ?? false;
        }

        public static bool IsAdmin(this ClaimsPrincipal principal) => FindFirstValue<bool>(principal, BasicClaimTypes.Admin) ?? principal.HasClaim("role", "Administrator");

        public static bool IsExternal(this ClaimsPrincipal principal) => principal.FindFirst("idp")?.Value != "local";
    }
}
