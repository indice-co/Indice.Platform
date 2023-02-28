using Microsoft.AspNetCore.Identity;

namespace FluentValidation;

/// <summary>Extensions that enhance the fluent validation with additional validator methods.</summary>
public static class FluentValidationExtensions
{
    /// <summary>Checks the given property against the list of allowed characters in the username configuration for the ASP.NET identity <see cref="UserOptions"/>. </summary>
    /// <typeparam name="T">The type of property.</typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    /// <param name="userOptions">Represents all user related options for the ASP.NET Identity. It retrieves the <see cref="UserOptions.AllowedUserNameCharacters"/>.</param>
    public static IRuleBuilderOptions<T, string> UserName<T>(this IRuleBuilder<T, string> ruleBuilder, UserOptions userOptions) =>
        ruleBuilder.Matches($"^[{userOptions.AllowedUserNameCharacters.Replace("-", "\\-")}]*$")
                   .WithMessage($"The field '{{PropertyName}}' has some invalid characters. Allowed characters are \"{userOptions.AllowedUserNameCharacters}\"");
}
