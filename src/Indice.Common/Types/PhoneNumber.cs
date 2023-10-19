#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;

namespace Indice.Globalization;

/// <summary>Encapsulates an International number format</summary>
public partial struct PhoneNumber : IFormattable
{
    private const string RegexPatternString = @"(\+(?<CallingCode>\d+(-\d+)?) (?<Number>[\d() -]{5,15}))|(?<GreekNumber>69\d{8})";
    private const char PlusSign = '+';
    private static readonly Regex _Pattern = PhoneNumberFormat();

    /// <summary>The calling code without the plus sign.</summary>
    public string CallingCode { get; }
    /// <summary>Two letter ISO country code.</summary>
    public string TwoLetterCountryCode { get; }
    /// <summary>Area code and Subscriber number</summary>
    public string Number { get; }
    /// <summary>Indicates that the Phone Number is a Greek Phone number.</summary>
    public readonly bool IsGreek => TwoLetterCountryCode == "GR";

    /// <summary>Create a format to parse and format a number</summary>
    public PhoneNumber(string callingCode, string twoLetterCountryCode, string number) {
        if (callingCode.Contains(PlusSign)) {
            throw new ArgumentException("Cannot contain the plus sign", nameof(callingCode));
        }
        if (twoLetterCountryCode.Length != 2) {
            throw new ArgumentException("should be exactly 2 characters long", nameof(twoLetterCountryCode));
        }
        CallingCode = callingCode;
        TwoLetterCountryCode = twoLetterCountryCode;
        Number = number;
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(RegexPatternString)]
    private static partial Regex PhoneNumberFormat();
#else 
    private static Regex PhoneNumberFormat() => new(RegexPatternString, RegexOptions.Compiled);
#endif

    /// <inheritdoc/>
    public override string ToString() => ToString("G");

    /// <summary>
    /// Parses the given phone number using.
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    public static PhoneNumber Parse(string phoneNumber) {
        if (phoneNumber is null) {
            throw new ArgumentNullException(nameof(phoneNumber));
        }
        var match = _Pattern.Match(phoneNumber);
        if (!match.Success) {
            throw new FormatException("The phoneNumber supplied was not in the correct format. Expected ie +30 6900000000");
        }
        // check for greek mobile phone
        if (match.Groups["GreekNumber"].Success) {
            return new PhoneNumber("30", "GR", match.Groups["GreekNumber"].Value);
        }
        var callingCode = match.Groups["CallingCode"].Value;
        var number = match.Groups["Number"].Value;
        if (!CountryInfo.TryGetCountryByCallingCode(callingCode, out var country)) {
            throw new FormatException($"The phoneNumber supplied has a calling code that is not corralate to a known country. Code '{callingCode}'");
        }
        return new PhoneNumber(callingCode, country.TwoLetterCode, number.Replace("-", "")
                                                                         .Replace("(", "")
                                                                         .Replace(")", "")
                                                                         .Replace(" ", ""));
    }

    /// <summary>Tries to convert the specified <paramref name="textInput"/> to a <see cref="PhoneNumber"/>.</summary>
    /// <param name="textInput">The phone number string to convert.</param>
    /// <param name="phoneNumber">The converted <see cref="PhoneNumber"/>.</param>
    /// <returns>True if conversion is successful, otherwise false.</returns>
    public static bool TryParse(string textInput, out PhoneNumber phoneNumber) {
        phoneNumber = default;
        try {
            phoneNumber = Parse(textInput);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>Converts a Phone Number to String of the corresponding format.</summary>
    /// <param name="format">
    /// - G: The generic format (ex. +30 69xxxxxxxx)
    /// - A: No plus sign format (ex. 30 69xxxxxxxx)
    /// - D: Digits only format (ex. 3069xxxxxxx)
    /// - N: Only the number without the calling code (ex. 69xxxxxxxx)
    /// </param>
    /// <returns></returns>
    public readonly string ToString(string format)=> ToString(format, PhoneNumberFormatInfo.Default);

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) {
        if (string.IsNullOrEmpty(format)) {
            format = "G";
        }
        formatProvider ??= PhoneNumberFormatInfo.Default;
        var x = string.Format(formatProvider, $"{{0:{format}}}", this);
        return x;
    }

    /// <summary>Implicit cast from <see cref="PhoneNumber"/> to <seealso cref="string"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(PhoneNumber value) => value.ToString();
    /// <summary>Explicit cast from <see cref="string"/> to <seealso cref="PhoneNumber"/></summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator PhoneNumber(string value) => Parse(value);
}
#nullable disable