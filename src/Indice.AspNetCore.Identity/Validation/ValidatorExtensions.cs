using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Validation
{
    /// <summary>
    /// Extensions that enhance the fluent validation with additional validator methods.
    /// </summary>
    public static class ValidatorExtensions
    {
        /// <summary>
        /// Check the taxid number for format &amp; checksum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleBuilder"></param>
        /// <param name="userOptions">Represents all user related options for the ASP.NET Identity. It retrieves the <see cref="UserOptions.AllowedUserNameCharacters"/></param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, string> UserName<T>(this IRuleBuilder<T, string> ruleBuilder, UserOptions userOptions) =>
            ruleBuilder.Matches($"^[{userOptions.AllowedUserNameCharacters.Replace("-", "\\-")}]*$")
            .WithMessage($"The field '{{PropertyName}}' has some invalid characters. Allowed characters are \"{userOptions.AllowedUserNameCharacters}\"");
    }
}
