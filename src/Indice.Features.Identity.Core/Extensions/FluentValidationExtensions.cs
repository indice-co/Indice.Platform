using System.Text.RegularExpressions;
using Indice.Features.Identity.Core;
using Indice.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace FluentValidation;

/// <summary>Extensions that enhance the fluent validation with additional validator methods.</summary>
public static class FluentValidationExtensions
{
    /// <summary>Checks the given property against the list of allowed characters in the username configuration for the ASP.NET identity <see cref="UserOptions"/>. </summary>
    /// <typeparam name="T">The type of property.</typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    /// <param name="userOptions">Represents all user related options for the ASP.NET Identity. It retrieves the <see cref="UserOptions.AllowedUserNameCharacters"/>.</param>
    public static IRuleBuilderOptions<T, string?> UserName<T>(this IRuleBuilder<T, string?> ruleBuilder, UserOptions userOptions) =>
        ruleBuilder.Matches($"^[{userOptions.AllowedUserNameCharacters.Replace("-", "\\-")}]*$")
                   .WithMessage($"The field '{{PropertyName}}' has some invalid characters. Allowed characters are \"{userOptions.AllowedUserNameCharacters}\"");

    /// <summary>Checks the given property against the list of allowed characters in the username configuration for the ASP.NET identity <see cref="UserOptions"/>. </summary>
    /// <typeparam name="T">The type of property.</typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    /// <param name="configuration">The IConfiguration param to discover the <strong>User:PhoneNumberRegex</strong> setting.</param>
    /// <param name="callingCodesProvider">The provider for the supported Calling Codes.</param>
    public static IRuleBuilderOptions<T, string?> UserPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder, IConfiguration configuration, CallingCodesProvider callingCodesProvider) {
        var phoneNumberRegex = configuration.GetIdentityOption("User", "PhoneNumberRegex");
        var phoneNumberRegexMessage = configuration.GetIdentityOption("User", "PhoneNumberRegexMessage");
        if (string.IsNullOrEmpty(phoneNumberRegex)) {
            return ruleBuilder.Must(input => {
                if (string.IsNullOrWhiteSpace(input)) {
                    return true;
                }
                if (!PhoneNumber.TryParse(input, out var phoneNumber)) {
                    return false;
                }
                var callingCode = callingCodesProvider.GetCallingCode(phoneNumber.CallingCode);
                if (callingCode is null) {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(callingCode.Pattern)) {
                    return Regex.IsMatch(phoneNumber.Number, callingCode.Pattern);
                }
                if (phoneNumber.IsGreek) {
                    return phoneNumber.Number.Length == 10;
                }
                return true;
            });
        }
        return ruleBuilder.Matches(phoneNumberRegex)
           .WithMessage(phoneNumberRegexMessage ?? "The field '{PropertyName}' has invalid format.");
    }
}
