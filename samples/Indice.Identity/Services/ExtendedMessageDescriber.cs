using System;
using Indice.Services;
using Microsoft.Extensions.Localization;

namespace Indice.Identity.Services
{
    public class ExtendedMessageDescriber : MessageDescriber
    {
        private readonly IStringLocalizer<ExtendedMessageDescriber> _localizer;

        public ExtendedMessageDescriber(IStringLocalizer<ExtendedMessageDescriber> localizer) {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public override string PhoneNumberVerificationMessage(string token) => _localizer["SMS verification code is {0}.", token];

        public override string EmailUpdateMessageSubject => _localizer["Confirm your account"];

        public override string EmailUpdateMessageBody(string returnUrl, string userId, string token) => _localizer["Email verification code is {0}.", token];

        public override string PasswordIsCommon() => _localizer["Your password is very common to use."];

        public override string PasswordHasNonLatinChars() => _localizer["Password cannot contain non latin characters."];

        public override string PasswordIdenticalToUserName() => _localizer["Your password is identical to your username."];

        public override string PasswordRecentlyUsed() => _localizer["This password has been used recently."];
    }
}
