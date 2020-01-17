using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models the request to update the email for the user.
    /// </summary>
    public class UpdateUserEmailRequest
    {
        /// <summary>
        /// The new user email.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string Email { get; set; }
    }
}
