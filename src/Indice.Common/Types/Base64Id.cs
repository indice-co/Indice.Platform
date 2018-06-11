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
        public Guid Id { get; }

        public Base64Id(Guid id) => Id = id;

        public override int GetHashCode() => Id.GetHashCode();

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

        public static implicit operator string(Base64Id value) => value.ToString();

        public static implicit operator Guid(Base64Id value) => value.Id;

        public static explicit operator Base64Id(string value) => Parse(value);

        public static explicit operator Base64Id(Guid value) => new Base64Id(value);
    }

    /// <summary>
    /// Converter class for the Base64Id.
    /// </summary>
    public class Base64IdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                return Base64Id.Parse((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                return ((Base64Id)value).ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
