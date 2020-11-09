using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a request for recovering password.
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>
        /// The email.
        /// </summary>
        [Required]
        public string Email { get; set; }
    }
}
