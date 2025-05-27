using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the request of a user for phone number confirmation.</summary>
public class ConfirmPhoneNumberChangeRequest
{
    /// <summary>The new phone number that the user wants to confirm.</summary>
    [Required(AllowEmptyStrings = false)]
    [DisplayName("phoneNumber")]
    public string PhoneNumber { get; set; } = null!;
    /// <summary>The OTP token. </summary>
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; } = null!;
}
