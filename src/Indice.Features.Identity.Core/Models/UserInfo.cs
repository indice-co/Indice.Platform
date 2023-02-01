using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Features.Identity.Core.Models
{
    /// <summary>Models an application user when API provides info for a single user.</summary>
    public class SingleUserInfo : BasicUserInfo
    {
        /// <summary>Creates a new instance of <see cref="SingleUserInfo"/>.</summary>
        public SingleUserInfo() { }

        /// <summary>The names of the roles that the user belongs to.</summary>
        public List<string> Roles { get; set; } = new List<string>();
        /// <summary>User metadata expressed as claims.</summary>
        public IEnumerable<ClaimInfo> Claims { get; set; } = new List<ClaimInfo>();

        /// <summary>Creates a new instance of <see cref="SingleUserInfo"/> from a <see cref="User"/> object.</summary>
        /// <param name="user">The user instance.</param>
        public static SingleUserInfo FromUser(User user) => new() {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            PasswordExpirationPolicy = user.PasswordExpirationPolicy,
            IsAdmin = user.Admin,
            TwoFactorEnabled = user.TwoFactorEnabled,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Claims = user.Claims?.Select(x => new ClaimInfo {
                Id = x.Id,
                Type = x.ClaimType,
                Value = x.ClaimValue
            })
            .ToList()
        };
    }

    /// <summary>Models an application user with his basic info.</summary>
    public class BasicUserInfo
    {
        /// <summary>User's unique identifier.</summary>
        public string Id { get; set; }
        /// <summary>Indicates whether a user's email is confirmed or not.</summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>Indicates whether lockout feature is enabled for the user.</summary>
        public bool LockoutEnabled { get; set; }
        /// <summary>Indicates whether a user's phone number is confirmed or not.</summary>
        public bool PhoneNumberConfirmed { get; set; }
        /// <summary>Indicates whether two-factor authentication is enabled for the user.</summary>
        public bool TwoFactorEnabled { get; set; }
        /// <summary>The date-time where the user was created in the system.</summary>
        public DateTimeOffset CreateDate { get; set; }
        /// <summary>The date-time where the lockout period ends.</summary>
        public DateTimeOffset? LockoutEnd { get; set; }
        /// <summary>User's email address.</summary>
        public string Email { get; set; }
        /// <summary>User's phone number.</summary>
        public string PhoneNumber { get; set; }
        /// <summary>The username.</summary>
        public string UserName { get; set; }
        /// <summary>Indicates whether the user is forcefully blocked.</summary>
        public bool Blocked { get; set; }
        /// <summary>Represents the password expiration policy the value is measured in days.</summary>
        public PasswordExpirationPolicy? PasswordExpirationPolicy { get; set; }
        /// <summary>Indicates whether the user is a system administrator.</summary>
        public bool IsAdmin { get; set; }
        /// <summary>The number of failed login attempts for the user.</summary>
        public int AccessFailedCount { get; set; }
        /// <summary>Gets or sets the date and time, in UTC, when the user last signed in.</summary>
        public DateTimeOffset? LastSignInDate { get; set; }
        /// <summary>If set, it represents the date when the current password will expire.</summary>
        public DateTimeOffset? PasswordExpirationDate { get; set; }
    }

    /// <summary>Models an application user when retrieving a list.</summary>
    public class UserInfo : BasicUserInfo
    {
        /// <summary>User's first name.</summary>
        public string FirstName { get; set; }
        /// <summary>User's last name.</summary>
        public string LastName { get; set; }
    }

    /// <summary>Extension methods that are used to convert from <see cref="User"/> type to other DTOs.</summary>
    public static class UserMappingExtensions
    {
        /// <summary>Converts a type of <see cref="User"/> to <see cref="BasicUserInfo"/>.</summary>
        /// <param name="user">The instance to convert.</param>
        /// <returns>A new instance of <see cref="BasicUserInfo"/>.</returns>
        public static BasicUserInfo ToBasicUserInfo(this User user) => new() {
            AccessFailedCount = user.AccessFailedCount,
            Blocked = user.Blocked,
            CreateDate = user.CreateDate,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Id = user.Id,
            IsAdmin = user.Admin,
            LastSignInDate = user.LastSignInDate,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            PasswordExpirationDate = user.PasswordExpirationDate,
            PasswordExpirationPolicy = user.PasswordExpirationPolicy,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            UserName = user.UserName
        };

        /// <summary>
        /// Converts a type of <see cref="User"/> to <see cref="BasicUserInfo"/>.
        /// </summary>
        /// <param name="user">The instance to convert.</param>
        /// <returns>A new instance of <see cref="BasicUserInfo"/>.</returns>
        public static SingleUserInfo ToSingleUserInfo(this User user) => new() {
            AccessFailedCount = user.AccessFailedCount,
            Blocked = user.Blocked,
            Claims = user.Claims.Select(x => new ClaimInfo { 
                Id = x.Id, 
                Type = x.ClaimType, 
                Value = x.ClaimValue 
            }),
            CreateDate = user.CreateDate,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Id = user.Id,
            IsAdmin = user.Admin,
            LastSignInDate = user.LastSignInDate,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            PasswordExpirationDate = user.PasswordExpirationDate,
            PasswordExpirationPolicy = user.PasswordExpirationPolicy,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            UserName = user.UserName
        };
    }
}
