using System.ComponentModel;
using System.Globalization;

namespace Indice.Types;

/// <summary>Represents a geographic location.</summary>
[TypeConverter(typeof(GeoPointTypeConverter))]
public class GeoPoint
{
    /// <summary>Creates a new instance of the <see cref="GeoPoint"/> class.</summary>
    public GeoPoint() { }

    /// <summary>Creates a new instance of the <see cref="GeoPoint"/> class.</summary>
    public GeoPoint(double latitude, double longitude, double? elevation = null) {
        Latitude = latitude;
        Longitude = longitude;
        Elevation = elevation;
    }

    /// <summary>The latitude.</summary>
    public double Latitude { get; set; }
    /// <summary>The longitude.</summary>
    public double Longitude { get; set; }
    /// <summary>The elevation.</summary>
    public double? Elevation { get; set; }

    /// <summary>Default string representation. <strong>37.9908697,23.7208298</strong>.</summary>
    /// <remarks>Web standard latitude followed by longitude invariant separated by comma.</remarks>
    public override string ToString() => Elevation.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "{0:G},{1:G},{2:G}", Latitude, Longitude, Elevation)
            : string.Format(CultureInfo.InvariantCulture, "{0:G},{1:G}", Latitude, Longitude);

    /// <summary>Generates a SQL Server compatible string representation of the coordinates. <strong>POINT(23.7208298,37.9908697)</strong></summary>
    /// <remarks>SQL geopoint ToString() representation. This is reversed <strong>longitude</strong> first then <strong>latitude</strong> then optionaly elevation.</remarks>
    public string ToDbGeographyString() => Elevation.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "POINT ({1:G} {0:G} {2:G})", Latitude, Longitude, Elevation)
            : string.Format(CultureInfo.InvariantCulture, "POINT ({1:G} {0:G})", Latitude, Longitude);

    // https://developer.here.com/documentation/places/topics/location-contexts.html#location-contexts__position-format
    // https://tools.ietf.org/rfc/rfc5870
    /// <summary>
    /// The Geolocation header (implicit context) and at parameter (explicit context) specify a position as a 'geo' URI. 
    /// The position is given as comma-separated values for latitude and longitude (in the WGS 84 coordinate system), 
    /// and optionally altitude (in meters above sea level), and a semicolon-separated list of position parameters. [RFC5870]
    /// </summary>
    /// <remarks>i.e. <strong>geo:37.9908697,23.7208298;cgen=map</strong></remarks>
    public string ToHeaderGeographyString() => Elevation.HasValue ?
            string.Format(CultureInfo.InvariantCulture, "geo:{0:G},{1:G},{2:G};cgen=map", Latitude, Longitude, Elevation) :
            string.Format(CultureInfo.InvariantCulture, "geo:{0:G},{1:G};cgen=map", Latitude, Longitude);

    /// <summary>Calculates the distance in kilometers between two <see cref="GeoPoint"/> instances using the <b>Haversine formula.</b>.</summary>
    /// <param name="geoPoint">The geographical point to calculate the distance to.</param>
    /// <remarks>https://en.wikipedia.org/wiki/Haversine_formula</remarks>
    public double Distance(GeoPoint geoPoint) {
        double R = 6371; // Earth radius.
        var lat = Math.PI / 180 * (geoPoint.Latitude - Latitude);
        var lng = Math.PI / 180 * (geoPoint.Longitude - Longitude);
        var h1 = Math.Sin(lat / 2) * Math.Sin(lat / 2) + 
                 Math.Cos(Math.PI / 180 * Latitude) * Math.Cos(Math.PI / 180 * geoPoint.Latitude) * Math.Sin(lng / 2) * Math.Sin(lng / 2);
        var h2 = 2 * Math.Asin(Math.Min(1, Math.Sqrt(h1)));
        return R * h2;
    }

    /// <summary>Tries to parse the given <paramref name="latLong"/> into a <paramref name="point"/>. In case of exception it handles it and returns false.</summary>
    /// <param name="latLong">Latitude and longitude as a string.</param>
    /// <param name="point">The parsed <see cref="GeoPoint"/> instance.</param>
    public static bool TryParse(string? latLong, out GeoPoint? point) {
        point = null;
        try {
            point = Parse(latLong!);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>Parses a string representation of <see cref="GeoPoint"/>.</summary>
    /// <param name="latLong">Latitude and longitude as a string.</param>
    /// <remarks>The method can parse 3 distinct formats. 
    /// <br/><strong>37.9908697,23.7208298</strong> Web standard latitude followed by longitude invariant separated by comma.
    /// <br/><strong>geo:37.9908697,23.7208298;cgen=map</strong> [RFC5870] Geolocation header Web standard latitude followed by longitude invariant separated by comma.
    /// <br/><strong>POINT(23.7208298,37.9908697)</strong> SQL geopoint ToString() representation. This is reversed longitude then latitude then optionally elevation.
    /// </remarks>
    public static GeoPoint Parse(string latLong) {
        if (string.IsNullOrEmpty(latLong)) {
            throw new ArgumentOutOfRangeException(nameof(latLong));
        }
        string[] parts;
        if (latLong.StartsWith("POINT", StringComparison.OrdinalIgnoreCase)) {
            latLong = latLong.TrimStart('P', 'O', 'I', 'N', 'T', '(', ' ').TrimEnd(' ', ')');
            parts = latLong.Split(' ').ToArray();
            if (parts.Length >= 2) {
                var lat = parts[1];
                var lon = parts[0];
                parts[0] = lat;
                parts[1] = lon;
            }
        } else if (latLong.StartsWith("geo:", StringComparison.OrdinalIgnoreCase)) {
            latLong = latLong.TrimStart('g', 'e', 'o', ':', ' ').TrimEnd(' ');
            parts = latLong.Split(';')[0].Split(',');
        } else {
            parts = latLong.Split(',');
        }
        if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1])) {
            throw new ArgumentOutOfRangeException(nameof(latLong));
        }
        var point = new GeoPoint {
            Latitude = double.Parse(parts[0], CultureInfo.InvariantCulture),
            Longitude = double.Parse(parts[1], CultureInfo.InvariantCulture),
        };
        if (parts.Length >= 3) {
            point.Elevation = double.Parse(parts[2], CultureInfo.InvariantCulture);
        }
        return point;
    }

    /// <summary>Cast operator.</summary>
    /// <param name="value">The text to parse.</param>
    public static explicit operator GeoPoint(string? value) => string.IsNullOrEmpty(value) ? null! : Parse(value);
}

/// <summary>Type converter for converting between <see cref="GeoPoint"/> and <seealso cref="string"/></summary>
public class GeoPointTypeConverter : TypeConverter
{
    /// <summary>Overrides can convert to declare support for string conversion.</summary>
    /// <param name="context">Provides contextual information about a component, such as its container and property descriptor.</param>
    /// <param name="sourceType"></param>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
        if (sourceType == typeof(string)) {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>Supply conversion from <see cref="string"/> to <seealso cref="GeoPoint"/> otherwise use default implementation</summary>
    /// <param name="context">Provides contextual information about a component, such as its container and property descriptor.</param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        if (value is string) {
            return GeoPoint.Parse((string)value);
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <summary>Supply conversion from <see cref="GeoPoint"/> to <seealso cref="string"/> otherwise use default implementation</summary>
    /// <param name="context">Provides contextual information about a component, such as its container and property descriptor.</param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    /// <param name="destinationType"></param>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
        if (destinationType == typeof(string)) {
            return ((GeoPoint)value!).ToString();
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
