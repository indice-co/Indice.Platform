using System.Text.RegularExpressions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Services;

namespace Indice.Features.Identity.UI.Models;

/// <summary>MFA login view model.</summary>
public partial class MfaLoginViewModel<TUser> : MfaLoginInputModel where TUser : User, new()
{
    /// <summary>The user entity.</summary>
    public TUser User { get; set; } = new TUser();
    /// <summary>Allows to choose less secure authentication method for MFA, if possible.</summary>
    public bool AllowDowngradeAuthenticationMethod { get; set; }
    /// <summary>The authentication method that will be used for MFA.</summary>
    public AuthenticationMethod? AuthenticationMethod { get; set; }
    /// <summary>The delivery channel that will be used for MFA.</summary>
    public TotpDeliveryChannel? AuthenticationMethodDeliveryChannel => AuthenticationMethod?.GetDeliveryChannel();
    /// <summary>Indicates whether the current browser device already exists for the user.</summary>
    public bool IsExistingBrowser { get; set; }
    /// <summary>The hub connection url. 
    /// In case of push otp via push notification service the user will be notified for the approval of his request via signalR. 
    /// This is the hub connection for the browser.</summary>
    public string? HubConnectionUrl { get; set; }
    /// <summary>The error message that informs the user that mfa cannot be performed.</summary>
    public string? Error { get; set; }
    /// <summary>True if the error message is populated. This is an indicator that the mfa process is in deadlock.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    /// <summary>Indicates whether the user can resend the MFA code.</summary>
    /// <remarks>Resending the MFA code is only possible when there was no error when sending. Or if the user has not already used the maximum number of attempts.</remarks>
    public bool ResendEnabled { get; set; } = true;
    /// <summary>The phone number of the user, masked for security reasons.</summary>
    public string? PhoneNumberMasked => PhoneNumberAvailable ? GetMaskPhoneNumberRegex().Replace(User.PhoneNumber!, "X") : null;
    /// <summary>Indicates whether the phone number is available for the user.</summary>
    public bool PhoneNumberAvailable => !string.IsNullOrWhiteSpace(User?.PhoneNumber);

    [GeneratedRegex(@"\d(?!\d{0,1}$)")]
    public static partial Regex GetMaskPhoneNumberRegex();
}

/// <summary>MFA login view model.</summary>
public class MfaLoginViewModel : MfaLoginViewModel<User> { }
