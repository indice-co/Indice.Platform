using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Indice.Identity.Services
{
    public class ExtendedIdentityErrorDescriber : IdentityErrorDescriber
    {
        private readonly IStringLocalizer<ExtendedIdentityErrorDescriber> _localizer;

        public ExtendedIdentityErrorDescriber(IStringLocalizer<ExtendedIdentityErrorDescriber> localizer) {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public override IdentityError PasswordTooShort(int length) => new IdentityError {
            Code = nameof(IdentityErrorDescriber.PasswordTooShort),
            Description = _localizer["Passwords must be at least {0} characters.", length]
        };

        public override IdentityError PasswordRequiresUpper() => new IdentityError {
            Code = nameof(IdentityErrorDescriber.PasswordRequiresUpper),
            Description = _localizer["Passwords must have at least one uppercase ('A'-'Z')."]
        };

        public override IdentityError PasswordRequiresDigit() => new IdentityError {
            Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
            Description = _localizer["Passwords must have at least one digit ('0'-'9')."]
        };

        public override IdentityError PasswordRequiresLower() => new IdentityError {
            Code = nameof(IdentityErrorDescriber.PasswordRequiresLower),
            Description = _localizer["Passwords must have at least one lowercase ('a'-'z')."]
        };

        public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError {
            Code = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
            Description = _localizer["Passwords must have at least one non alphanumeric character."]
        };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new IdentityError {
            Code = nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
            Description = _localizer["Passwords must use at least {0} different characters.", uniqueChars]
        };
    }
}
