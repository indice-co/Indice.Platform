namespace Indice.Globalization;

/// <summary>A provider about <see cref="PhoneNumber"/> formatting.</summary>
public class PhoneNumberFormatInfo : IFormatProvider, ICustomFormatter
{
    /// <summary>The default phone number formatter</summary>
    public static PhoneNumberFormatInfo Default => new();

    /// <inheritdoc/>
    public string Format(string format, object arg, IFormatProvider formatProvider) {
        if (arg is not PhoneNumber) {
            return string.Format(format, arg);
        }
        var phoneNumber = (PhoneNumber)arg;
        return format.ToUpperInvariant() switch {
            "G" => $"+{phoneNumber.CallingCode} {phoneNumber.Number}",
            "A" => $"{phoneNumber.CallingCode} {phoneNumber.Number}",
            "D" => $"{phoneNumber.CallingCode.Replace("-", "")}{phoneNumber.Number}",
            "N" => $"{phoneNumber.Number}",
            _ => throw new FormatException(string.Format("The {0} format string is not supported.", format)),
        };
    }

    /// <inheritdoc/>
    public object GetFormat(Type formatType) {
        if (formatType == typeof(ICustomFormatter)) {
            return this;
        }
        return null;
    }
}
