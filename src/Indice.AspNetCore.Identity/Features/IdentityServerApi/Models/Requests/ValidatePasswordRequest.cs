using Indice.Types;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Class that models the request for validating a user's password.
    /// </summary>
    public class ValidatePasswordRequest
    {
        /// <summary>
        /// A token representing the user id.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// The password.
        /// </summary>
        public string Password { get; set; }
    }
}
