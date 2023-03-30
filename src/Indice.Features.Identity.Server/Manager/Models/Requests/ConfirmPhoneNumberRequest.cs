using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models.Requests;

/// <summary>Models the request of a user for phone number confirmation.</summary>
public class ConfirmPhoneNumberRequest
{
    /// <summary>The OTP token. </summary>
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; }
}
