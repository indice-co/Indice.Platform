using System.ComponentModel;
using System.Globalization;

namespace Indice.Features.Cases.Core.Models;

/// <summary>The Filter Term model.</summary>
[TypeConverter(typeof(FilterTermTypeConverter))]
public class FilterTerm
{

    /// <summary>FilterTerm's Key</summary>
    public string? Key { get; set; }

    /// <summary>FilterTerm's Value</summary>
    public string? Value { get; set; }

        /// <inheritdoc />
    public override int GetHashCode() =>
        (Key ?? string.Empty).GetHashCode() ^ (Value ?? string.Empty).GetHashCode();

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        if (obj is FilterTerm clause) {
            var other = clause;
            return other.Key == Key && other.Value == Value;
        }

        return base.Equals(obj);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Key}{Value}";

    /// <summary>
    /// Parses the specified extra data string into a <see cref="FilterTerm"/> object.
    /// </summary>
    /// <param name="extraData">The extra data string, expected in "Key Value" format.</param>
    /// <returns>The parsed <see cref="FilterTerm"/>, or a default instance if parsing fails.</returns>
    public static FilterTerm Parse(string? extraData)
    {
        if (string.IsNullOrWhiteSpace(extraData))
        {
            // Return a default FilterTerm if input is null or empty
            return new FilterTerm();
        }

        var data = extraData.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (data.Length == 2)
        {
            // Return a new FilterTerm if input is in "Key Value" format
            return new FilterTerm { Key = data[0], Value = data[1] };
        }

        // Return a default FilterTerm if input is not in the expected format
        return new FilterTerm();
    }

    /// <summary>
    /// Tries to parse the specified filter string.
    /// </summary>
    /// <param name="filter">The filter string.</param>
    /// <param name="FilterTerm">The parsed <see cref="FilterTerm"/>.</param>
    /// <returns>true if the parse operation was successful; otherwise, false.</returns>
    public static bool TryParse(string? filter, out FilterTerm? FilterTerm) {
        FilterTerm = default;
        try {
            FilterTerm = Parse(filter);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="FilterTerm"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="FilterTerm"/> value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator string(FilterTerm value) => value.ToString();

    /// <summary>
    /// Performs an explicit conversion from <see cref="string"/> to <see cref="FilterTerm"/>.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator FilterTerm(string? value) => Parse(value);

    /// <summary>
    /// Determines whether two specified instances of <see cref="FilterTerm"/> are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>true if the two values are equal; otherwise, false.</returns>
    public static bool operator ==(FilterTerm left, FilterTerm right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="FilterTerm"/> are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>true if the two values are not equal; otherwise, false.</returns>
    public static bool operator !=(FilterTerm left, FilterTerm right) => !(left == right);
}

/// <summary>
/// A type converter for <see cref="FilterTerm"/>.
/// </summary>
public class FilterTermTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
        if (sourceType == typeof(string)) {
            return true;
        }

        return base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        if (value is string @string) {
            return FilterTerm.Parse(@string);
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
        Type destinationType) {
        if (destinationType == typeof(string)) {
            return ((FilterTerm) value!).ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
