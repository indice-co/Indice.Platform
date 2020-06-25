using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models the payload when user requests a phone number update.
    /// </summary>
    public class UpdateUserPhoneNumberRequest
    {
        /// <summary>
        /// The phone number.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string PhoneNumber { get; set; }
    }
}
