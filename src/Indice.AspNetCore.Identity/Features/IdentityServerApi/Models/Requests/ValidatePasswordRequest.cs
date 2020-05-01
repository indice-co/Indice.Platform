using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Class that models the request for validating a user's password.
    /// </summary>
    public class ValidatePasswordRequest
    {
        /// <summary>
        /// The username.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// The password.
        /// </summary>
        [Required]
        public string Password { get; set; }
    }
}
