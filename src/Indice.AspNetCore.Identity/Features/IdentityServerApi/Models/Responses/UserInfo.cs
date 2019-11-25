using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models an application user when API provides info for a single user.
    /// </summary>
    public class SingleUserInfo : BasicUserInfo
    {
        /// <summary>
        /// The names of the roles that the user belongs to.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Models an application user with his basic info.
    /// </summary>
    public class BasicUserInfo
    {
        /// <summary>
        /// User's unique identifier.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Indicates whether a user's email is confirmed or not.
        /// </summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// Indicates whether lockout feature is enabled for the user.
        /// </summary>
        public bool LockoutEnabled { get; set; }
        /// <summary>
        /// Indicates whether a user's phone number is confirmed or not.
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }
        /// <summary>
        /// Indicates whether two-factor authentication is enabled for the user.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }
        /// <summary>
        /// The datetime where the user was created in the system.
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
        /// <summary>
        /// The datetime where the lockout period ends.
        /// </summary>
        public DateTimeOffset? LockoutEnd { get; set; }
        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// User's phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// The username.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// User metadata expressed as claims.
        /// </summary>
        public List<ClaimInfo> Claims { get; set; } = new List<ClaimInfo>();
    }

    /// <summary>
    ///  Models an application user when retrieving a list.
    /// </summary>
    public class UserInfo : BasicUserInfo
    {
        /// <summary>
        /// User's first name.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName { get; set; }
    }
}
