using System.Collections.Generic;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>MFA login view model.</summary>
    public class MfaLoginViewModel<TUser> : MfaLoginInputModel where TUser : User
    {
        /// <summary>The delivery channel that should be used to send the TOTP.</summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; }
        /// <summary>The user entity.</summary>
        public TUser User { get; set; }
        /// <summary>Allows to choose less secure channel for MFA, if possible.</summary>
        public bool AllowMfaChannelDowngrade { get; set; }
        public IEnumerable<string> DeviceNames { get; set; } = new List<string>();
    }

    /// <summary>MFA login view model.</summary>
    public class MfaLoginViewModel : MfaLoginViewModel<User> { }
}
