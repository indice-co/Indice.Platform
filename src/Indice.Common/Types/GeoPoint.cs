using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Types
{
    /// <summary>
    /// Geographic location
    /// </summary>
    [TypeConverter(typeof(GeoPointTypeConverter))]
    public class GeoPoint
    {
        /// <summary>
        /// The latitude
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The logitude
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Default string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, "{0:G},{1:G}", Latitude, Longitude);
        }

        /// <summary>
        /// Generates a SqlServer compatible string representation of the coordinates.
        /// </summary>
        /// <returns></returns>
        public string ToDbGeographyString() {
            return string.Format(CultureInfo.InvariantCulture, "POINT ({1:G} {0:G})", Latitude, Longitude);
        }

        // https://developer.here.com/documentation/places/topics/location-contexts.html#location-contexts__position-format
        // https://tools.ietf.org/rfc/rfc5870
        /// <summary>
        /// The Geolocation header (implicit context) and at parameter (explicit context) specify a position as a 'geo' URI. 
        /// The position is given as comma-separated values for latitude and longitude (in the WGS 84 coordinate system), 
        /// and optionally altitude (in meters above sea level), and a semicolon-separated list of position parameters. [RFC5870]
        /// </summary>
        /// <returns></returns>
        public string ToHeaderGeographyString() {
            return string.Format(CultureInfo.InvariantCulture, "geo:{0:G},{1:G};cgen=map", Latitude, Longitude);
        }


        /// <summary>
        /// Tries to parse the given <paramref name="latlong"/> into a <paramref name="point"/>. In case of exception it handles it and returns false
        /// </summary>
        /// <param name="latlong"></param>
        /// <param name="point"></param>
        /// <returns>true in case of success</returns>
        public static bool TryParse(string latlong, out GeoPoint point) {
            point = null;
            try {
                point = Parse(latlong);
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Parses a string representation of <see cref="GeoPoint"/>.
        /// </summary>
        /// <param name="latlong"></param>
        /// <returns></returns>
        public static GeoPoint Parse(string latlong) {
            if (string.IsNullOrEmpty(latlong)) {
                throw new ArgumentOutOfRangeException(nameof(latlong));
            }
            var parts = new string[0];
            if (latlong.StartsWith("POINT", StringComparison.OrdinalIgnoreCase)) {
                latlong = latlong.TrimStart('P', 'O', 'I', 'N', 'T', '(', ' ').TrimEnd(' ', ')');
                parts = latlong.Split(' ').Reverse().ToArray();
            } else if (latlong.StartsWith("geo:", StringComparison.OrdinalIgnoreCase)) {
                latlong = latlong.TrimStart('g', 'e', 'o', ':', ' ').TrimEnd(' ');
                parts = latlong.Split(';')[0].Split(',');
            } else {
                parts = latlong.Split(',');
            }
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1])) {
                throw new ArgumentOutOfRangeException(nameof(latlong));
            }
            return new GeoPoint() {
                Latitude = double.Parse(parts[0], CultureInfo.InvariantCulture),
                Longitude = double.Parse(parts[1], CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// Cast Operator 
        /// </summary>
        /// <param name="value">the text to parse</param>
        public static explicit operator GeoPoint(string value) {
            if (string.IsNullOrEmpty(value)) return null;

            return Parse(value);
        }
    }

    /// <summary>
    /// Type converter for converting between <see cref="GeoPoint"/> and <seealso cref="string"/>
    /// </summary>
    public class GeoPointTypeConverter : TypeConverter
    {
        /// <summary>
        /// Overrides can convert to declare support for string conversion.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Supply conversion from <see cref="string"/> to <seealso cref="GeoPoint"/> otherwise use default implementation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                return GeoPoint.Parse((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Supply conversion from <see cref="GeoPoint"/> to <seealso cref="string"/> otherwise use default implementation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                return ((GeoPoint)value).ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
