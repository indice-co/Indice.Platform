using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>MFA login view model.</summary>
    public class MfaLoginViewModel<TUser> : MfaLoginInputModel where TUser : User
    {
        /// <summary>The delivery channel that should be used to send the TOTP.</summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; }
        /// <summary></summary>
        public TUser User { get; set; }
    }

    /// <summary>MFA login view model.</summary>
    public class MfaLoginViewModel : MfaLoginViewModel<User> { }
}
