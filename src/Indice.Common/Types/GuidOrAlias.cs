using System.Buffers.Text;
using System.ComponentModel;
using System.Globalization;
using Indice.Extensions;

namespace Indice.Types;

/// <summary>
/// Converts a guid back and forth to a url safe base64 string.
/// Use this class to wrap a Guid into a representiation that is shortened and obfuscated for querystring use. 
/// </summary>
[TypeConverter(typeof(GuidOrAliasTypeConverter))]
public struct GuidOrAlias
{
    /// <summary>The actual value.</summary>
    public string Value { get; }
    /// <summary>
    /// The actual value as a <see cref="Guid"/>.
    /// </summary>
    public readonly Guid Uuid => IsGuid ? Guid.Parse(Value) : Guid.Empty;

    /// <summary>Test value for GUID.</summary>
    public readonly bool IsGuid => Guid.TryParse(Value, out var _);

    /// <summary>Construct the type from a <see cref="string" />.</summary>
    /// <param name="value"></param>
    public GuidOrAlias(string value) {
        ArgumentException.ThrowIfNullOrEmpty(value);
        value = value.Trim();
        if (Guid.TryParse(value, out var guid)) {
            value = guid.ToString();
        }
        Value = value;
    }

    /// <summary>Construct the type from a <see cref="Guid" />.</summary>
    /// <param name="guid"></param>
    public GuidOrAlias(Guid guid) => Value = guid.ToString();

    /// <inheritdoc/>
    public override readonly int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) {
        if (obj is GuidOrAlias id) {
            var other = id;
            return other.Value.Equals(Value);
        }
        return base.Equals(obj);
    }

    /// <summary>Gets the inner guid as a url safe base64 string.</summary>
    /// <returns></returns>
    public override readonly string ToString() => Value;

    /// <summary>Parse from a string url safe base64 representation. </summary>
    /// <param name="input">The string to convert.</param>
    public static GuidOrAlias Parse(string input) {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        return new GuidOrAlias(input);
    }

    /// <summary>Tries to convert the specified <paramref name="base64"/> to a <see cref="GuidOrAlias"/>.</summary>
    /// <param name="base64">The base64 string to convert.</param>
    /// <param name="GuidOrAlias">The converted <see cref="GuidOrAlias"/>.</param>
    /// <returns>True if conversion is successful, otherwise false.</returns>
    public static bool TryParse(string base64, out GuidOrAlias GuidOrAlias) {
        GuidOrAlias = default;
        try {
            GuidOrAlias = Parse(base64);
            return true;
        } catch (FormatException) {
            return false;
        } catch (ArgumentNullException) {
            return false;
        }
    }

    /// <summary>Implicit cast from <see cref="GuidOrAlias"/> to <seealso cref="string"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(GuidOrAlias value) => value.Value;
    /// <summary>Implicit cast from <see cref="GuidOrAlias"/> to <seealso cref="Guid"/></summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Guid(GuidOrAlias value) => value.Uuid;


    /// <summary>implicit cast from <see cref="Guid"/> to <seealso cref="GuidOrAlias"/></summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator GuidOrAlias(Guid value) => new GuidOrAlias(value);

    /// <summary>Explicit cast from <see cref="string"/> to <seealso cref="GuidOrAlias"/></summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator GuidOrAlias(string value) => Parse(value);
}

/// <summary>Converter class for the <see cref="GuidOrAlias"/>.</summary>
public class GuidOrAliasTypeConverter : TypeConverter
{
    /// <summary>Overrides can convert to declare support for string conversion.</summary>
    /// <param name="context"></param>
    /// <param name="sourceType"></param>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
        if (sourceType == typeof(string)) {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>Supply conversion from <see cref="string"/> to <seealso cref="GuidOrAlias"/> otherwise use default implementation.</summary>
    /// <param name="context"></param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        if (value is string stringValue) {
            return GuidOrAlias.Parse(stringValue);
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <summary>from <seealso cref="GuidOrAlias"/> to <see cref="string"/> otherwise use default implementation.</summary>
    /// <param name="context"></param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    /// <param name="destinationType"></param>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
        if (destinationType == typeof(string)) {
            return ((GuidOrAlias)value!).ToString();
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
