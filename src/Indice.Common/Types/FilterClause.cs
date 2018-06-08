using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Indice.Types
{
    /// <summary> 
    /// Used to represent generic filter expressions for unstructured Json data.
    /// Example: "date::eq::(datetime)2018-10-28"
    /// </summary>
    [TypeConverter(typeof(FilterClauseTypeConverter))]
    public struct FilterClause
    {

        const string REGEX_PATTERN = @"^\s*([A-Za-z_][A-Za-z0-9_\.]+)::({0})::(\(({1})\))?(.+)\s*$";
        static readonly Regex parseRegex = new Regex(string.Format(REGEX_PATTERN, 
                                                                   string.Join("|", Enum.GetNames(typeof(FilterOperator))).ToLowerInvariant(),
                                                                   string.Join("|", Enum.GetNames(typeof(JsonDataType))).ToLowerInvariant()
                                                     ), RegexOptions.IgnoreCase);
        
        /// <summary>
        /// Member path to compare
        /// </summary>
        public string Member { get; }

        /// <summary>
        /// Value to compare against
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The operator to apply between the <see cref="Member" /> and <seealso cref="Value"/>
        /// </summary>
        public FilterOperator Operator { get; }

        /// <summary>
        /// The <see cref="JsonDataType"/> of the data <seealso cref="Member"/>
        /// </summary>
        public JsonDataType DataType { get; }

        /// <summary>
        /// Creates the Json filter by supplying all the required members.
        /// </summary>
        /// <param name="member">Member path to compare</param>
        /// <param name="value">Value to compare against</param>
        /// <param name="operator">The operator to apply between the <paramref name="member"/> and <paramref name="value"/></param>
        /// <param name="dataType">The <see cref="JsonDataType"/> of the data <paramref name="member"/></param>
        public FilterClause(string member, string value, FilterOperator @operator, JsonDataType dataType) {
            Member = member;
            Value = value;
            Operator = @operator;
            DataType = dataType;
        }

        /// <summary>
        /// Returnes a hash code for the value of this instance.
        /// </summary>
        /// <returns>An integer representing the hash code for the value of this instance.</returns>
        public override int GetHashCode() => (Member ?? string.Empty).GetHashCode() ^
                                             (Value ?? string.Empty).GetHashCode() ^
                                             Operator.GetHashCode() ^
                                             DataType.GetHashCode();

        /// <summary>
        /// Indicates whether this instance and a specified object are equal. 
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (obj != null && obj is FilterClause) {
                var other = ((FilterClause)obj);
                return other.Member == Member &&
                       other.Value == Value &&
                       other.Operator == Operator &&
                       other.DataType == DataType;
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// The string representation of the <see cref="FilterClause"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Member}::{Operator}::({DataType}){Value}";

        /// <summary>
        /// Parse the string representation to an instance of <see cref="FilterClause"/>
        /// </summary>
        /// <param name="filter">The string representation to parse.</param>
        /// <returns></returns>
        public static FilterClause Parse(string filter) {
            if (filter != null) {
                var match = parseRegex.Match(filter);
                if (match.Success) {
                    Enum.TryParse<FilterOperator>(match.Groups[2].Value, true, out var @operator);
                    Enum.TryParse<JsonDataType>(match.Groups[4].Value, true, out var jsonType);
                    return new FilterClause(match.Groups[1].Value, match.Groups[5].Value, @operator, jsonType);
                }
            }
            return new FilterClause();
        }

    }

    /// <summary>
    /// <see cref="FilterClause"/> <seealso cref="TypeConverter"/> is used for aspnet rout binding to work from.
    /// </summary>
    public class FilterClauseTypeConverter : TypeConverter
    {
        /// <summary>
        /// Overrides the ConvertTo method of TypeConverter.
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
        /// Overrides the ConvertFrom method of TypeConverter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                return FilterClause.Parse((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Overrides the ConvertTo method of TypeConverter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                return ((FilterClause)value).ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
