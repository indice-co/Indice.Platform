using System;
using System.ComponentModel;
using System.Globalization;
using Indice.Extensions;

namespace Indice.Types
{
    /// <summary>
    /// Converts a guid back and forth to a url safe base64 string.
    /// Use this class to wrap a Guid into a representiation that is shortened and obfuscated for querystring use. 
    /// </summary>
    [TypeConverter(typeof(Base64IdTypeConverter))]
    public struct Base64Id
    {
        /// <summary>
        /// The internal <see cref="Id"/> value.
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// Construct the type from a <see cref="Guid" />.
        /// </summary>
        /// <param name="id"></param>
        public Base64Id(Guid id) => Id = id;

        /// <summary>
        /// Returns the hashcode for this instance
        /// </summary>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Compare equality with the giver object. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (obj != null && obj is Base64Id) {
                var other = ((Base64Id)obj);
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// Gets the inner guid as a url safe base64 string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Id.ToByteArray().ToBase64UrlSafe();

        /// <summary>
        /// Parse from a string url safe base64 representation. 
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static Base64Id Parse(string base64) {
            if (base64 != null) {
                if (Guid.TryParse(base64, out Guid id)) {
                    return new Base64Id(id);
                }
                var guid = new Guid(base64.FromBase64UrlSafe());
                return new Base64Id(guid);
            } else {
                return new Base64Id();
            }
        }

        /// <summary>
        /// Implicit cast from <see cref="Base64Id"/> to <seealso cref="string"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator string(Base64Id value) => value.ToString();
        /// <summary>
        /// Implicit cast from <see cref="Base64Id"/> to <seealso cref="Guid"/>
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator Guid(Base64Id value) => value.Id;
        /// <summary>
        /// Explicit cast from <see cref="string"/> to <seealso cref="Base64Id"/>
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static explicit operator Base64Id(string value) => Parse(value);
        /// <summary>
        /// Explicit cast from <see cref="Guid"/> to <seealso cref="Base64Id"/>
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static explicit operator Base64Id(Guid value) => new Base64Id(value);
    }

    /// <summary>
    /// Converter class for the <see cref="Base64Id"/>.
    /// </summary>
    public class Base64IdTypeConverter : TypeConverter
    {
        /// <summary>
        /// Overrides can convert to declare support for string conversion.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Supply conversion from <see cref="string"/> to <seealso cref="Base64Id"/> otherwise use default implementation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                return Base64Id.Parse((string)value);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// from <seealso cref="Base64Id"/> to <see cref="string"/> otherwise use default implementation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                return ((Base64Id)value).ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
