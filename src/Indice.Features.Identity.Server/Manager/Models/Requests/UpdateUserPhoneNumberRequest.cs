using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the payload when user requests a phone number update.</summary>
public class UpdateUserPhoneNumberRequest
{
    /// <summary>The phone number.</summary>
    [Required(AllowEmptyStrings = false)]
    [DisplayName("phoneNumber")]
    public string PhoneNumber { get; set; } = null!;
    /// <summary></summary>
    public string? DeliveryChannel { get; set; } = "Sms";
}
