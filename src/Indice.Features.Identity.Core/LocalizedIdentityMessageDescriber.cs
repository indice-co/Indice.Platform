using System.Runtime;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core;

/// <summary>A version of <see cref="IdentityMessageDescriber"/> that uses the localizer</summary>
public class LocalizedIdentityMessageDescriber : IdentityMessageDescriber
{
    private readonly IStringLocalizer<LocalizedIdentityMessageDescriber> _localizer;
    private readonly IConfiguration _configuration;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="localizer">The localizer</param>
    /// <param name="configuration">Settings</param>
    /// <exception cref="ArgumentNullException"></exception>
    public LocalizedIdentityMessageDescriber(IStringLocalizer<LocalizedIdentityMessageDescriber> localizer, IConfiguration configuration) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    /// <inheritdoc/>
    public override string PhoneNumberVerificationMessage(string token) => _localizer["SMS verification code is {0}.", token];
    /// <inheritdoc/>
    public override string UpdateEmailMessageSubject => _localizer["Confirm your account"];
    /// <inheritdoc/>
    public override string UpdateEmailMessageBody<TUser>(TUser user, string token, string returnUrl) => _localizer["Email verification code is {0}.", token];
    /// <inheritdoc/>
    public override string PasswordIsCommon => _localizer["Password is very easy to guess, please choose a more complex one."];
    /// <inheritdoc/>
    public override string PasswordHasNonLatinChars => _localizer["Password cannot contain non-Latin characters."];
    /// <inheritdoc/>
    public override string PasswordIdenticalToUserName => _localizer["Your password is identical to your username."];
    /// <inheritdoc/>
    public override string PasswordRecentlyUsed => _localizer["This password has been used recently."];
    /// <inheritdoc/>
    public override string PasswordHasNonLatinCharsRequirement => _localizer["Does not contain non-Latin characters."];
    /// <inheritdoc/>
    public override string PasswordIsCommonRequirement => _localizer["Not easy to guess."];
    /// <inheritdoc/>
    public override string PasswordIdenticalToUserNameRequirement => _localizer["Does not contain part of your username."];
    /// <inheritdoc/>
    public override string PasswordRecentlyUsedRequirement => _localizer["Not recently used."];
    /// <inheritdoc/>
    public override string ForgotPasswordMessageBody<TUser>(TUser user, string token) {
        var u = user as User;
        var url = $"{_configuration.GetHost()}/forgot-password/confirmation?email={Uri.EscapeDataString(u.Email)}&token={Uri.EscapeDataString(token)}";
        var body = $"Παρακαλούμε δημιουργήστε το νέο σας κωδικό <a href=\"{url}\">ακολουθώντας τον εξής σύνδεσμο</a>.";
        return body;
    }
}
