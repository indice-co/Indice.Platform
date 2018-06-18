using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Sample Grant Validator for a custom grant_type. (like password, client_credentials, etc)
    /// </summary>
    public class NoSubjectExtensionGrantValidator : IExtensionGrantValidator
    {
        /// <summary>
        /// Validates the context for the grant type "custom.nosubject".
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task ValidateAsync(ExtensionGrantValidationContext context) {
            var credential = context.Request.Raw.Get("custom_credential");

            if (credential != null) {
                context.Result = new GrantValidationResult();
            } else {
                // Custom error message.
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid custom credentials.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// The grant type
        /// </summary>
        public string GrantType => "custom.nosubject";
    }
}
