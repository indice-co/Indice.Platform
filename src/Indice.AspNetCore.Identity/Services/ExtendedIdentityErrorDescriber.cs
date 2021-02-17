using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Extends the default <see cref="IdentityErrorDescriber"/> adding hints for password validation rules.
    /// </summary>
    public class ExtendedIdentityErrorDescriber : IdentityErrorDescriber
    {
        /// <summary>
        /// Your password's specified length does not meet the minimum length requirements.
        /// </summary>
        public virtual string PasswordTooShortRequirement(int length) => string.Format(Resources.Culture, Resources.PasswordTooShortRequirement, length);
        /// <summary>
        /// Your password must meet the minimum number of unique chars required.
        /// </summary>
        public virtual string PasswordRequiresUniqueCharsRequirement(int uniqueChars) => string.Format(Resources.Culture, Resources.PasswordRequiresUniqueCharsRequirement, uniqueChars);
        /// <summary>
        /// Your password must contain a non-alphanumeric character, which is required by the password policy.
        /// </summary>
        public virtual string PasswordRequiresNonAlphanumericRequirement => string.Format(Resources.Culture, Resources.PasswordRequiresNonAlphanumericRequirement);
        /// <summary>
        /// Your password must contain a numeric character, which is required by the password policy.
        /// </summary>
        public virtual string PasswordRequiresDigitRequirement => string.Format(Resources.Culture, Resources.PasswordRequiresDigitRequirement);
        /// <summary>
        /// Your password must contain a lower case letter, which is required by the password policy.
        /// </summary>
        public virtual string PasswordRequiresLowerRequirement => string.Format(Resources.Culture, Resources.PasswordRequiresLowerRequirement);
        /// <summary>
        /// Your password must contain an upper case letter, which is required by the password policy.
        /// </summary>
        public virtual string PasswordRequiresUpperRequirement => string.Format(Resources.Culture, Resources.PasswordRequiresUpperRequirement);
    }
}
