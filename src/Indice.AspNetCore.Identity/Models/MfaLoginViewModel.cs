using Indice.Services;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>MFA login view model.</summary>
    public class MfaLoginViewModel : MfaLoginInputModel
    {
        /// <summary>The delivery channel that should be used to send the TOTP.</summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; }
    }
}
