using Indice.Services;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>View model used during MFA login.</summary>
    public class MfaLoginViewModel
    {
        /// <summary>The OTP code inserted by the user.</summary>
        public string OtpCode { get; set; }
        /// <summary>The delivery channel that should be used to send the TOTP.</summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; }
        /// <summary>The return URL after the login is successful.</summary>
        public string ReturnUrl { get; set; }
        /// <summary>Flag that indicates that the device performed the login should be persisted.</summary>
        public bool RememberDevice { get; set; }
    }
}
