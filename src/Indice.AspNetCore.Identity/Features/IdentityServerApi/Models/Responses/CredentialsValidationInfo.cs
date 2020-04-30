using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models the result of validating a user's credentials.
    /// </summary>
    public class CredentialsValidationInfo
    {
        /// <summary>
        /// Determines if the provided username is already taken by another user.
        /// </summary>
        public bool? UserNameExists { get; set; }
        /// <summary>
        /// Contains the results of checking various password validation rules.
        /// </summary>
        public IList<PasswordRuleInfo> PasswordRules { get; set; }
    }

    /// <summary>
    /// Models a password validation rule.
    /// </summary>
    public class PasswordRuleInfo
    {
        /// <summary>
        /// The name of the rule checked.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Determines if rule validation was successful or not.
        /// </summary>
        public bool IsValid { get; set; }
    }
}
