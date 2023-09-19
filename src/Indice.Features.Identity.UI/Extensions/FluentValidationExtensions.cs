using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Types;
using Indice.Globalization;

namespace FluentValidation;

/// <summary>
/// Extensions that enhance the fluent validation with additional validator methods.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Checks the given properties against a list of country calling codes in the <see cref="IdentityUIOptions"/> configuration.
    /// </summary>
    /// <typeparam name="T">The type of property.</typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    /// <param name="callingCodeSelector">Selects the calling code from the model.</param>
    /// <param name="options">The <see cref="IdentityUIOptions"/> to discover the enabled countries.</param>
    public static IRuleBuilderOptions<T, string?> UserGlobalPhoneNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        Func<T, string?> callingCodeSelector,
        IdentityUIOptions options) {

        var phoneCountries = options.PhoneCountries
            ?? throw new InvalidOperationException($"The {nameof(IdentityUIOptions.PhoneCountries)} list of {nameof(IdentityUIOptions)} was null.");

        var allowedCodes = CountryInfo.Countries
                .IntersectBy(phoneCountries, x => x.TwoLetterCode)
                .SelectMany(x => x.CallingCode.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .ToHashSet();

        return ruleBuilder
            .Must((input, phoneNumber) =>
                callingCodeSelector(input) is { } callingCode
                && phoneNumber is not null
                && allowedCodes.Contains(callingCode)
                && PhoneInfo.TryParse($"+{callingCode.TrimStart('+')}{phoneNumber}", "GR", out var phone)
                && PhoneInfo.IsValidNumber(phone))
            .WithMessage("The field '{PropertyName}' has invalid format.");
    }
}
