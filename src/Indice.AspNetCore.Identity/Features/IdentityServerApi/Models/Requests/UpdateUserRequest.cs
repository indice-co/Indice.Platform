using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a user that will be updated on the server.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Indicates whether lockout feature is enabled for the user.
        /// </summary>
        public bool LockoutEnabled { get; set; }
        /// <summary>
        /// Indicates whether two-factor authentication is enabled for the user.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }
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
        /// Dynamic claims that have been marked as required.
        /// </summary>
        public List<BasicUserClaimInfo> Claims { get; set; } = new List<BasicUserClaimInfo>();
    }
}
