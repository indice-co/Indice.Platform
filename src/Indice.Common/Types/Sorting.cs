using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Indice.Types;

/// <summary>Object Representation for a sort by clause</summary>
[TypeConverter(typeof(SortByClauseTypeConverter))]
public struct SortByClause
{
    const string REGEX_PATTERN = @"^\s*(\(({0})\))?([A-Za-z_][A-Za-z0-9_\.]+)(\+|-)?\s*$";
    static readonly Regex parseRegex = new Regex(string.Format(REGEX_PATTERN,
                                                               string.Join("|", Enum.GetNames(typeof(JsonDataType))).ToLowerInvariant()
                                                 ), RegexOptions.IgnoreCase);
    /// <summary>Sort by ascending reserved constant</summary>
    public const string ASC = nameof(ASC);

    /// <summary>Sort by descending reserved constant</summary>
    public const string DESC = nameof(DESC);

    /// <summary>Creates an instance of a SortByClause</summary>
    public SortByClause(string path, string direction, JsonDataType? dataType = null) {
        if (direction != ASC && direction != DESC) {
            throw new ArgumentOutOfRangeException(nameof(direction), $"Invalid direction {direction}. Possible values are {ASC} or {DESC}");
        }
        Path = path;
        Direction = direction;
        DataType = dataType;
    }

    /// <summary>
    /// The <see cref="JsonDataType"/> of the data located at the specified <seealso cref="Path"/>. 
    /// This is optional.
    /// </summary>
    public JsonDataType? DataType { get; }

    /// <summary>the property path</summary>
    public string Path { get; }

    /// <summary>the sort direction (ASC, DESC)</summary>
    public string Direction { get; }

    /// <summary>Gets the string representation. ex Name+</summary>
    /// <returns></returns>
    public override string ToString() {
        var text = Path + (Direction == DESC ? "-" : "+");
        if (DataType.HasValue)
            return $"({DataType.Value.ToString().ToLowerInvariant()}){text}";
        return text;        
    }

    /// <summary>Returns a hash code for the value of this instance.</summary>
    /// <returns>An integer representing the hash code for the value of this instance.</returns>
    public override int GetHashCode() => (Path ?? string.Empty).GetHashCode() ^
                                         (Direction ?? string.Empty).GetHashCode() ^
                                         (DataType ?? 0).GetHashCode();

    /// <summary>Indicates whether this instance and a specified object are equal. </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns></returns>
    public override bool Equals(object? obj) {
        if (obj != null && obj is SortByClause) {
            var other = ((SortByClause)obj);
            return other.Path == Path &&
                   other.Direction == Direction &&
                   other.DataType == DataType;
        }
        return base.Equals(obj);
    }

    /// <summary>
    /// Moves the current state of this sort clause to the next state.
    /// Say if we had sort by "Name ASC" it cycles between ASC DESC and none each time it is called.
    /// It returns a new <see cref="SortByClause"/> and does not change the current reference.
    /// </summary>
    /// <returns></returns>
    public SortByClause? NextState() {
        switch (Direction) {
            case ASC:
                return new SortByClause(Path, DESC, DataType);
            case DESC:
                return null;
            default:
                throw new Exception($"unexpected direction {Direction}");
        }
    }

    /// <summary>Parse the string representation  for a sort path with direction into a <see cref="SortByClause"/></summary>
    /// <param name="text">a property path (case agnostic) followed by a sing '+' or '-'.</param>
    /// <returns></returns>
    public static SortByClause Parse(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            throw new ArgumentOutOfRangeException(nameof(text));
        }

        var match = parseRegex.Match(text);
        if (!match.Success) {
            throw new ArgumentOutOfRangeException(nameof(text));
        }

        var dataType = default(JsonDataType?);
        if (Enum.TryParse<JsonDataType>(match.Groups[2].Value, true, out var dt)) {
            dataType = dt;
        }
        return new SortByClause(match.Groups[3].Value,
                                match.Groups[4].Value == "-" ? DESC : ASC,
                                dataType);
    }

    /// <summary>Implicit cast from <see cref="SortByClause"/> to <see cref="string" /></summary>
    /// <param name="value"></param>
    public static implicit operator string(SortByClause value) => value.ToString();

    /// <summary>Explicit cast from <see cref="string "/> to <see cref="SortByClause" /></summary>
    /// <param name="value"></param>
    public static explicit operator SortByClause(string value) => Parse(value);
    
}


/// <summary><see cref="SortByClause"/> <seealso cref="TypeConverter"/> is used for aspnet rout binding to work from.</summary>
public class SortByClauseTypeConverter : TypeConverter
{
    /// <summary>Overrides the ConvertTo method of TypeConverter.</summary>
    /// <param name="context"></param>
    /// <param name="sourceType"></param>
    /// <returns></returns>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
        if (sourceType == typeof(string)) {
            return true;
        }

        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>Overrides the ConvertFrom method of TypeConverter.</summary>
    /// <param name="context"></param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        if (value is string) {
            return SortByClause.Parse((string)value);
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <summary>Overrides the ConvertTo method of TypeConverter.</summary>
    /// <param name="context"></param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    /// <param name="destinationType"></param>
    /// <returns></returns>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
        if (destinationType == typeof(string)) {
            return ((SortByClause)value!).ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
