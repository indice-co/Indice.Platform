using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>A view model for the MFA onboarding completion page.</summary>
public class MfaOnboardingCompleteViewModel
{
    /// <summary>The authentication method that was enabled.</summary>
    public AuthenticationMethodType Method { get; set; }
    /// <summary>The masked email or phone number the method delivers codes to. Null when not applicable (e.g. authenticator app).</summary>
    public string? MaskedDestination { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Creates a new <see cref="MfaOnboardingCompleteViewModel"/>, masking the destination.</summary>
    /// <param name="method">The authentication method that was enabled.</param>
    /// <param name="destination">The email or phone number. Will be masked.</param>
    /// <param name="returnUrl">The return URL.</param>
    public static MfaOnboardingCompleteViewModel Create(AuthenticationMethodType method, string? destination, string? returnUrl) => new() {
        Method = method,
        MaskedDestination = method switch {
            AuthenticationMethodType.Email => MaskEmail(destination),
            AuthenticationMethodType.PhoneNumber => MaskPhone(destination),
            _ => null
        },
        ReturnUrl = returnUrl
    };

    private static string? MaskEmail(string? email) {
        if (string.IsNullOrWhiteSpace(email)) {
            return null;
        }
        var at = email.IndexOf('@');
        if (at <= 0) {
            return email;
        }
        return $"{email[0]}***{email[at..]}";
    }

    private static string? MaskPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone)) {
            return null;
        }
        const int visible = 4;
        if (phone.Length <= visible) {
            return phone;
        }
        return new string('*', phone.Length - visible) + phone[^visible..];
    }
}
