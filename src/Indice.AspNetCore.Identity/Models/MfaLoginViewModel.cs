using System.Collections.Generic;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Models;

/// <summary>MFA login view model.</summary>
public class MfaLoginViewModel<TUser> : MfaLoginInputModel where TUser : User
{
    /// <summary>The delivery channel that should be used to send the TOTP.</summary>
    public TotpDeliveryChannel DeliveryChannel { get; set; }
    /// <summary>The user entity.</summary>
    public TUser User { get; set; }
    /// <summary>Allows to choose less secure channel for MFA, if possible.</summary>
    public bool AllowMfaChannelDowngrade { get; set; }
    /// <summary>The list of name devices that will be notified in case the <see cref="DeliveryChannel"/> is <seealso cref="TotpDeliveryChannel.PushNotification"/>.</summary>
    public IEnumerable<string> DeviceNames { get; set; } = new List<string>();
}

/// <summary>MFA login view model.</summary>
public class MfaLoginViewModel : MfaLoginViewModel<User> { }
