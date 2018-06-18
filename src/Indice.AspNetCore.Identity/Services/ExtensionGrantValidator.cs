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
    public class ExtensionGrantValidator : IExtensionGrantValidator
    {
        /// <summary>
        /// Context validation for grant type "custom"
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task ValidateAsync(ExtensionGrantValidationContext context) {
            var credential = context.Request.Raw.Get("custom_credential");

            if (credential != null) {
                context.Result = new GrantValidationResult(subject: "818727", authenticationMethod: "custom");
            } else {
                // Custom error message.
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid custom credentials.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// The grant type
        /// </summary>
        public string GrantType => "custom";
    }
}
