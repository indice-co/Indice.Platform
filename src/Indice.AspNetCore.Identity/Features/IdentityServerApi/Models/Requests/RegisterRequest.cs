using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a new user that is registering on the system.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// The first name of the user.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// The last name of the user.
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// The username used to login.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// User password.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// User password confirmation.
        /// </summary>
        public string PasswordConfirmation { get; set; }
        /// <summary>
        /// Email.
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// Privacy policy read.
        /// </summary>
        public bool HasReadPrivacyPolicy { get; set; }
        /// <summary>
        /// Terms read.
        /// </summary>
        public bool HasAcceptedTerms { get; set; }
        /// <summary>
        /// User claims.
        /// </summary>
        public List<BasicClaimInfo> Claims { get; set; }
    }
}
