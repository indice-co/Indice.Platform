using System;
using System.Collections.Generic;
using System.Linq;
using Indice.Security;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Represents a user in the Identity system.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        public User() : this(string.Empty, Guid.NewGuid()) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        /// <remarks>The Id property is initialized to from a new GUID string value.</remarks>
        /// <param name="userName">The user name</param>
        public User(string userName) : base(userName) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        public User(string userName, Guid id) : base(userName) {
            Id = id.ToString();
            UserName = userName;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        public User(string userName, string id) : base(userName) {
            Id = id;
            UserName = userName;
        }

        /// <summary>
        /// Date that the user was created
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
        /// <summary>
        /// Gets or sets the date and time, in UTC, when the user last signed in.
        /// </summary>
        public DateTimeOffset? LastSignInDate { get; set; }
        /// <summary>
        /// Date that represents the last time the user changed his password.
        /// </summary>
        public DateTimeOffset? LastPasswordChangeDate { get; set; }
        /// <summary>
        /// Represents the password expiration policy the value is measured in days.
        /// </summary>
        public PasswordExpirationPolicy? PasswordExpirationPolicy { get; set; }
        /// <summary>
        /// If set, it represents the date when the current password will expire.
        /// </summary>
        public DateTimeOffset? PasswordExpirationDate { get; set; }
        /// <summary>
        /// System administrator Indicator.
        /// </summary>
        public bool Admin { get; set; }
        /// <summary>
        /// Indicates whether the user is forcefully blocked.
        /// </summary>
        public bool Blocked { get; set; }
        /// <summary>
        /// Indicates whether the user must provide a new password upon next login.
        /// </summary>
        public bool PasswordExpired { get; set; }
        /// <summary>
        /// Navigation property for the roles this user belongs to. This setting takes precedence over <see cref="PasswordExpirationPolicy"/>.
        /// </summary>
        public virtual ICollection<IdentityUserRole<string>> Roles { get; } = new List<IdentityUserRole<string>>();
        /// <summary>
        /// Navigation property for the claims this user possesses.
        /// </summary>
        public virtual ICollection<IdentityUserClaim<string>> Claims { get; } = new List<IdentityUserClaim<string>>();
        /// <summary>
        /// Navigation property for this users login accounts.
        /// </summary>
        public virtual ICollection<IdentityUserLogin<string>> Logins { get; } = new List<IdentityUserLogin<string>>();

        /// <summary>
        /// Calculates the next date that the user must change his password. Th
        /// </summary>
        public DateTimeOffset? CalculatePasswordExpirationDate() {
            if (!PasswordExpirationPolicy.HasValue) {
                return null;
            }
            var lastChange = LastPasswordChangeDate ?? DateTime.UtcNow;
            return PasswordExpirationPolicy.Value switch {
                Models.PasswordExpirationPolicy.Never => null,
                Models.PasswordExpirationPolicy.Monthly => lastChange.AddMonths(1),
                Models.PasswordExpirationPolicy.Quarterly => lastChange.AddMonths(3),
                Models.PasswordExpirationPolicy.Semesterly => lastChange.AddMonths(6),
                Models.PasswordExpirationPolicy.Annually => lastChange.AddMonths(12),
                Models.PasswordExpirationPolicy.Biannually => lastChange.AddMonths(24),
                _ => lastChange.AddDays((int)PasswordExpirationPolicy.Value)
            };
        }

        /// <summary>
        /// Check to see if the current password has expired according to current password expiration policy.
        /// </summary>
        /// <param name="now">The date to use as now.</param>
        public bool HasExpiredPassword(DateTime? now = null) {
            var expired = false;
            now ??= DateTime.UtcNow;
            if (PasswordExpirationPolicy.HasValue) {
                var expirationDate = CalculatePasswordExpirationDate();
                expired = expirationDate.HasValue && expirationDate <= now;
            }
            return expired;
        }
    }

    /// <summary>
    /// Helper methods for <see cref="User"/> type.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// Adds the developer-totp claim to the provided user instance and provides a random 6-digit code.
        /// </summary>
        /// <param name="user">Represents a user in the Identity system.</param>
        public static void GenerateDeveloperTotp(this User user) {
            var developerTotpClaims = user.Claims.Where(x => x.ClaimType == BasicClaimTypes.DeveloperTotp).ToList();
            if (developerTotpClaims?.Count() > 0) {
                foreach (var claim in developerTotpClaims) {
                    user.Claims.Remove(claim);
                }
            }
            var developerTotp = new Random(Convert.ToInt32(DateTime.UtcNow.ToString("ffffff"))).Next(100000, 999999);
            user.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = BasicClaimTypes.DeveloperTotp,
                ClaimValue = developerTotp.ToString(),
                UserId = user.Id
            });
        }
    }
}
