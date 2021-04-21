using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models the result of validating a user's credentials.
    /// </summary>
    public class CredentialsValidationInfo
    {
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
        public string Code { get; set; }
        /// <summary>
        /// The rule error description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The requirement for the rule.
        /// </summary>
        public string Requirement { get; set; }
        /// <summary>
        /// Determines if rule validation was successful or not.
        /// </summary>
        public bool IsValid { get; set; }
    }
}
