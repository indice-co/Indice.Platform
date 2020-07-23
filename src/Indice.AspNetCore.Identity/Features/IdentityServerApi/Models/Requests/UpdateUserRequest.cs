using System.Collections.Generic;
using Indice.AspNetCore.Identity.Models;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a user that will be updated on the server.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Indicates whether two-factor authentication is enabled for the user.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }
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
        public List<BasicClaimInfo> Claims { get; set; } = new List<BasicClaimInfo>();
        /// <summary>
        /// Represents the password expiration policy the value is measured in days.
        /// </summary>
        public PasswordExpirationPolicy? PasswordExpirationPolicy { get; set; }
        /// <summary>
        /// Indicates whether the user is a system administrator.
        /// </summary>
        public bool IsAdmin { get; set; }
        /// <summary>
        /// Indicates whether a user's email is confirmed or not.
        /// </summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// Indicates whether a user's phone number is confirmed or not.
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }
    }
}
