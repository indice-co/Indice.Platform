﻿#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Indice.Globalization;

/// <summary>Encapsulates an International number format</summary>
[DebuggerDisplay("{ToString(),nq} ({TwoLetterCountryCode,nq})")]
public partial struct PhoneNumber : IFormattable
{

    //[StringSyntax("Regex")] uncomment after .net 6 drop
    private const string RegexPatternString = @"(\+(?<CallingCode>\d+(-\d+)?) (?<Number>[\d() -]{5,15}))|(?<GreekNumber>69\d{8})|(?<GreekNumber>69\d{8})|(?<GreekNumberLand>2\d{9})|(?<InternationalNumber>((00)|\+)?\s?\d{10,15})||(?<UnidentifiedNumber>\d{5,9})";
                                                                                                                                                                                                        //^\+[0-9]{1,3}\.[0-9]{4,14}(?:x.+)?$
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
        // check for greek land phone
        if (match.Groups["GreekNumberLand"].Success) {
            return new PhoneNumber("30", "GR", match.Groups["GreekNumberLand"].Value);
        }
        string callingCode;
        string number;
        CountryInfo? country;
        // check for international phone with double zero start
        if (match.Groups["InternationalNumber"].Success) {
            var internationalNumber = match.Groups["InternationalNumber"].Value;
            country = CountryInfo.FindCountriesByPhoneNumber(internationalNumber).FirstOrDefault();
            if (country is null) {
                throw new FormatException($"The phoneNumber supplied was identified as an unknown international number '{internationalNumber}'");
            }
            callingCode = country.CallingCodeDefault.ToString();
            return new PhoneNumber(callingCode, country.TwoLetterCode, internationalNumber
                                                                        .TrimStart('+', '0')
                                                                        .Substring(callingCode.Length)
                                                                        .Replace("-", "")
                                                                        .Replace("(", "")
                                                                        .Replace(")", "")
                                                                        .Replace(" ", ""));
        }
        // check for Unidentified phone
        if (match.Groups["UnidentifiedNumber"].Success) {
            var unknown = match.Groups["UnidentifiedNumber"].Value;
            throw new FormatException($"The phoneNumber supplied was identified as an unknown number '{unknown}'");
        }
        callingCode = match.Groups["CallingCode"].Value;
        number = match.Groups["Number"].Value;
        if (!CountryInfo.TryGetCountryByCallingCode(callingCode, out country)) {
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
    /// - O: The generic no whitespace format (ex. +3069xxxxxxxx)
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
        return string.Format(formatProvider, $"{{0:{format}}}", this);
    }

    /// <summary>Implicit cast from <see cref="PhoneNumber"/> to <seealso cref="string"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(PhoneNumber value) => value.ToString();
    /// <summary>Explicit cast from <see cref="string"/> to <seealso cref="PhoneNumber"/></summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator PhoneNumber(string value) => Parse(value);
}
#nullable disable