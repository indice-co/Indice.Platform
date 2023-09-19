using System.Diagnostics.CodeAnalysis;
using PhoneNumbers;

namespace Indice.Features.Identity.UI.Types;

/// <summary>
/// Contains information about the phone number. Based google's phonenumber lib.
/// </summary>
public static class PhoneInfo
{
    private static PhoneNumberUtil Util => PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Returns all countries/regions in two-letter format that are supported.
    /// </summary>
    public static HashSet<string> AllCountries => Util.GetSupportedRegions();

    /// <summary>
    /// Parses a string and returns it as a <see cref="PhoneNumber"/>. This method does not validate a number.
    /// </summary>
    /// <remarks>This populates the raw_input and country_code+source fields.</remarks>
    /// <param name="phone">Number that we are attempting to parse. This can contain formatting such as +, ( and -, as well as a phone number extension.</param>
    /// <param name="defaultRegion">
    /// Region the number is expected to be from. This is only used if the number is not written in international format.
    /// The country calling code for the number in this case would be stored as that of the default region supplied.
    /// </param>
    /// <param name="phoneNumber"></param>
    /// <returns><see langword="true"/> if the number was parsed otherwise false.</returns>
    public static bool TryParse(string? phone, string defaultRegion, [NotNullWhen(true)] out PhoneNumber? phoneNumber) {
        phoneNumber = default;

        if (phone is null) return false;

        try {
            phoneNumber = Util.ParseAndKeepRawInput(phone, defaultRegion);
            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    /// Tests whether a phone number matches a valid pattern. This doesn't verify
    /// the number is actually in use, which is impossible to tell by just looking at a number itself.
    /// </summary>
    /// <param name="phone">The phone number that we want to validate.</param>
    /// <returns><see langword="true"/> if the number is of a valid pattern otherwise false.</returns>
    public static bool IsValidNumber(PhoneNumber phone) {
        return Util.IsValidNumber(phone);
    }

    /// <summary>
    /// Formats a phone number in the specified format using default rules.
    /// This does not promise to produce a phone number that the user can dial from where
    /// they are - although we do format in either 'national' or 'international' format
    /// depending on what the client asks for, we do not currently support a more abbreviated
    /// format, such as for users in the same "area" who could potentially dial the number
    /// without area code. Note that if the phone number has a country calling code of
    /// 0 or an otherwise invalid country calling code, we cannot work out which formatting
    /// rules to apply so we return the national significant number with no formatting applied.
    /// </summary>
    /// <param name="phone">The phone number to be formatted.</param>
    /// <param name="format">The format the phone number should be formatted into. Defaults to (E164).</param>
    /// <returns>The formatted phone number.</returns>
    /// <exception cref="NumberParseException">When the number cannot be parsed.</exception>
    public static string Format(string phone, PhoneNumberFormat format = PhoneNumberFormat.E164) {
        var phoneNumber = Util.Parse(phone, null);
        return Util.Format(phoneNumber, format);
    }

    /// <summary>
    /// Formats a phone number in the specified format using default rules.
    /// This does not promise to produce a phone number that the user can dial from where
    /// they are - although we do format in either 'national' or 'international' format
    /// depending on what the client asks for, we do not currently support a more abbreviated
    /// format, such as for users in the same "area" who could potentially dial the number
    /// without area code. Note that if the phone number has a country calling code of
    /// 0 or an otherwise invalid country calling code, we cannot work out which formatting
    /// rules to apply so we return the national significant number with no formatting applied.
    /// </summary>
    /// <param name="phoneNumber">The phone number to be formatted.</param>
    /// <param name="format">The format the phone number should be formatted into. Defaults to (E164).</param>
    /// <returns>The formatted phone number.</returns>
    public static string Format(PhoneNumber phoneNumber, PhoneNumberFormat format = PhoneNumberFormat.E164) {
        return Util.Format(phoneNumber, format);
    }
}
