using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a request for changing the username.
    /// </summary>
    public class UpdateUserNameRequest
    {
        /// <summary>
        /// The new username.
        /// </summary>
        [Required]
        public string UserName { get; set; }
    }
}
