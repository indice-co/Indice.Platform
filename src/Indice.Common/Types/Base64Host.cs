﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Indice.Types
{
    /// <summary>
    /// Converts a base uri host back and forth to a url safe base64 string.
    /// Use this class to wrap a Guid into a representiation that is shortened and obfuscated for querystring use. 
    /// </summary>
    [TypeConverter(typeof(Base64HostTypeConverter))]
    public struct Base64Host
    {
#if NETSTANDARD14
        private const string UriSchemeHttps = "https";
        private const string UriSchemeHttp = "http";
        private const string SchemeDelimiter = "://";
#else
        private static readonly string UriSchemeHttps = Uri.UriSchemeHttps;
        private static readonly string UriSchemeHttp = Uri.UriSchemeHttp;
        private static readonly string SchemeDelimiter = Uri.SchemeDelimiter;
#endif
        /// <summary>
        /// The internal <see cref="Host"/> value.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Construct the type from a <see cref="Uri" />
        /// </summary>
        /// <param name="uri"></param>
        public Base64Host(Uri uri) {
#if NETSTANDARD14
            Host = uri.OriginalString.Replace(uri.PathAndQuery, string.Empty);
#else
            Host = uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority);
#endif
        }

        /// <summary>
        /// Construct the type from a <see cref="string"/> Url.
        /// </summary>
        /// <param name="uri"></param>
        public Base64Host(string uri) : this(new Uri(uri, UriKind.Absolute)) { }

        /// <summary>
        /// Returns the hashcode for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Host.GetHashCode();

        /// <summary>
        /// Compare equality with the giver object. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (obj != null && obj is Base64Host host) {
                var other = host;
                return other.Host == Host;
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// Gets the inner guid as a url safe base64 string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var host = new Uri(Host, UriKind.Absolute);
            var schemeByte = host.Scheme == UriSchemeHttps ? (byte)1 : (byte)0;
            FromShort((ushort)host.Port, out var portByte1, out var portByte2);
            var data = new List<byte> {
                schemeByte,
                portByte1,
                portByte2
            };
            data.AddRange(Encoding.ASCII.GetBytes(host.Host));
            var base64 = Convert.ToBase64String(data.ToArray());
            return base64;
        }

        /// <summary>
        /// Parse from a string url safe base64 representation. 
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static Base64Host Parse(string base64) {
            if (base64 != null) {
                var data = Convert.FromBase64String(base64);
                var isHttps = data[0].Equals(1);
                var port = ToShort(data[1], data[2]);
                var hostBytes = new byte[data.Length - 3];
                Array.Copy(data, 3, hostBytes, 0, data.Length - 3);
                var host = Encoding.ASCII.GetString(hostBytes);
                var sb = new StringBuilder();
                var isDefaultPort = (isHttps && port == 443) || (!isHttps && port == 80);
                sb.AppendFormat("{0}{1}{2}", isHttps ? UriSchemeHttps : UriSchemeHttp, SchemeDelimiter, host);
                if (!isDefaultPort) {
                    sb.AppendFormat(":{0}", port);
                }
                return new Base64Host(sb.ToString());
            } else {
                return new Base64Host();
            }
        }

        private static ushort ToShort(byte byte1, byte byte2) {   // using Int32 because that is what all the operations return anyway...
            return (ushort)((((int)byte1) << 8) | (int)byte2);
        }

        private static void FromShort(ushort number, out byte byte1, out byte byte2) {
            byte1 = (byte)(number >> 8); // to treat as same byte 1 from above
            byte2 = (byte)number;
        }
    }

    /// <summary>
    /// Converter class for the <see cref="Base64Id"/>.
    /// </summary>
    public class Base64HostTypeConverter : TypeConverter
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
        /// Supply conversion from <see cref="string"/> to <seealso cref="Base64Host"/> otherwise use default implementation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string @string) {
                return Base64Host.Parse(@string);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// from <seealso cref="Base64Host"/> to <see cref="string"/> otherwise use default implementation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                return ((Base64Host)value).ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
