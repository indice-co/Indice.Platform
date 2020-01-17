using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models the request to update the phone number for the user.
    /// </summary>
    public class UpdateUserPhoneRequest
    {
        /// <summary>
        /// The new phone number.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string PhoneNumber { get; set; }
    }
}
