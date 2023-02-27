using System;
using Indice.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Indice.Identity.Services;

public class LocalizedIdentityMessageDescriber : IdentityMessageDescriber
{
    private readonly IStringLocalizer<LocalizedIdentityMessageDescriber> _localizer;

    public LocalizedIdentityMessageDescriber(IStringLocalizer<LocalizedIdentityMessageDescriber> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public override string PhoneNumberVerificationMessage(string token) => _localizer["SMS verification code is {0}.", token];
    public override string UpdateEmailMessageSubject => _localizer["Confirm your account"];
    public override string UpdateEmailMessageBody<TUser>(TUser user, string token, string returnUrl) => _localizer["Email verification code is {0}.", token];
    public override string PasswordIsCommon => _localizer["Password is very easy to guess, please choose a more complex one."];
    public override string PasswordHasNonLatinChars => _localizer["Password cannot contain non-Latin characters."];
    public override string PasswordIdenticalToUserName => _localizer["Your password is identical to your username."];
    public override string PasswordRecentlyUsed => _localizer["This password has been used recently."];
    public override string PasswordHasNonLatinCharsRequirement => _localizer["Does not contain non-Latin characters."];
    public override string PasswordIsCommonRequirement => _localizer["Not easy to guess."];
    public override string PasswordIdenticalToUserNameRequirement => _localizer["Does not contain part of your username."];
    public override string PasswordRecentlyUsedRequirement => _localizer["Not recently used."];
}
