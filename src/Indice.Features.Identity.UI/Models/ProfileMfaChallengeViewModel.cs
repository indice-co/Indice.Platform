using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for the MFA challenge dialog displayed in the change email and change phone number pages.</summary>
public class ProfileMfaChallengeViewModel
{
    /// <summary>The MFA challenge state of the current request.</summary>
    public ProfileMfaChallengeStatus Status { get; set; }
    /// <summary>The methods the user can use for the MFA challenge, ordered by security level (most secure first).</summary>
    public IReadOnlyList<AuthenticationMethod> Methods { get; set; } = [];
    /// <summary>The selected method.</summary>
    public AuthenticationMethod? SelectedMethod { get; set; }
    /// <summary>Indicates whether the user may pick a different method than the preferred one.</summary>
    public bool AllowMethodSelection { get; set; }
    /// <summary>Indicates that a code has been sent through the delivery channel of the selected method.</summary>
    public bool CodeSent { get; set; }
    /// <summary>The number of digits of the code.</summary>
    public int CodeLength { get; set; } = 6;
    /// <summary>Indicates that the user has MFA enabled but no usable method, so changes are blocked.</summary>
    public bool IsBlocked => Status == ProfileMfaChallengeStatus.Blocked || SelectedMethod is null;
    /// <summary>Indicates that the selected method needs a code to be sent before verification (SMS, Viber, email).</summary>
    public bool RequiresSending => SelectedMethod?.SupportsDeliveryChannel() == true;
    /// <summary>Indicates that the code input should be displayed.</summary>
    public bool CanEnterCode => !IsBlocked && (!RequiresSending || CodeSent);
    /// <summary>The methods, other than the selected one, the user can switch to.</summary>
    public IEnumerable<AuthenticationMethod> AlternativeMethods => AllowMethodSelection
        ? Methods.Where(method => !string.Equals(method.Code, SelectedMethod?.Code, StringComparison.OrdinalIgnoreCase))
        : [];
}
