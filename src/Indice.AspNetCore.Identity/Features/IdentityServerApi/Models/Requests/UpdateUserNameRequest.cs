using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Api.Models
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
