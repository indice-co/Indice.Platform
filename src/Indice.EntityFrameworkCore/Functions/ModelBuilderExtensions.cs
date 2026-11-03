using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Indice.EntityFrameworkCore.Functions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Indice.EntityFrameworkCore;

/// <summary>Extension methods for json <see cref="ModelBuilder"/></summary>
public static class ModelBuilderExtensions
{
    /// <summary>Applies configuration for all <see cref="JsonFunctions"/> using the <see cref="SqlDialect.SqlServer"/> dialect.</summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static ModelBuilder ApplyJsonFunctions(this ModelBuilder modelBuilder) => modelBuilder.ApplyJsonFunctions(SqlDialect.SqlServer);

    /// <summary>Applies configuration for all <see cref="JsonFunctions"/> resolving the <see cref="SqlDialect"/> from the Entity Framework Core provider name.</summary>
    /// <param name="modelBuilder"></param>
    /// <param name="providerName">The Entity Framework Core provider name. Usually <c>DbContext.Database.ProviderName</c>.</param>
    /// <returns></returns>
    public static ModelBuilder ApplyJsonFunctions(this ModelBuilder modelBuilder, string? providerName) => modelBuilder.ApplyJsonFunctions(SqlDialectExtensions.GetSqlDialect(providerName));

    /// <summary>Applies configuration for all <see cref="JsonFunctions"/></summary>
    /// <param name="modelBuilder"></param>
    /// <param name="dialect">The database engine to target.</param>
    /// <returns></returns>
    public static ModelBuilder ApplyJsonFunctions(this ModelBuilder modelBuilder, SqlDialect dialect) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        modelBuilder.ApplyJsonValue(dialect);
        modelBuilder.ApplyJsonCastToDate(dialect);
        modelBuilder.ApplyJsonCastToDouble(dialect);
        return modelBuilder;
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.JsonValue(object, string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonValue(this ModelBuilder modelBuilder) => modelBuilder.ApplyJsonValue(SqlDialect.SqlServer);

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.JsonValue(object, string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <param name="dialect">The database engine to target.</param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonValue(this ModelBuilder modelBuilder, SqlDialect dialect) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        var functionBuilder = modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonValue))!);
        switch (dialect) {
            case SqlDialect.PostgreSql:
                functionBuilder.HasTranslation(TranslatePostgreSqlJsonValue);
                functionBuilder.HasParameter("column").Metadata.TypeMapping = new StringTypeMapping("text", null);
                break;
            default:
                functionBuilder.HasTranslation(args => {
                    return new SqlFunctionExpression("JSON_VALUE", args, nullable: true, argumentsPropagateNullability: args.Select(x => true), typeof(string), null);
                });
                functionBuilder.HasParameter("column").Metadata.TypeMapping = new StringTypeMapping("NVARCHAR(MAX)", null);
                break;
        }
        functionBuilder.HasSchema(string.Empty);
        return functionBuilder;
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDate(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonCastToDate(this ModelBuilder modelBuilder) => modelBuilder.ApplyJsonCastToDate(SqlDialect.SqlServer);

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDate(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <param name="dialect">The database engine to target.</param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonCastToDate(this ModelBuilder modelBuilder, SqlDialect dialect) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        var functionBuilder = modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDate))!);
        switch (dialect) {
            case SqlDialect.PostgreSql:
                functionBuilder.HasTranslation(TranslatePostgreSqlCastToDate);
                break;
            default:
                functionBuilder.HasTranslation(TranslateSqlServerCastToDate);
                break;
        }
        return functionBuilder.HasSchema(string.Empty);
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDouble(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonCastToDouble(this ModelBuilder modelBuilder) => modelBuilder.ApplyJsonCastToDouble(SqlDialect.SqlServer);

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDouble(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <param name="dialect">The database engine to target.</param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonCastToDouble(this ModelBuilder modelBuilder, SqlDialect dialect) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        var functionBuilder = modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDouble))!);
        switch (dialect) {
            case SqlDialect.PostgreSql:
                functionBuilder.HasTranslation(args => new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(double), new DoubleTypeMapping("double precision")));
                break;
            default:
                functionBuilder.HasTranslation(args => {
                    var float32 = new SqlFragmentExpression("float");
                    var convertArgs = new SqlExpression[] { float32 }.Concat(args);
                    return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(double?), null);
                });
                break;
        }
        return functionBuilder.HasSchema(string.Empty);
    }

    /// <summary>
    /// Translates to <c>CAST(regexp_replace(value, '&lt;date without offset&gt;', '\1Z') AS timestamp with time zone)</c> so that the result does not depend on the session <c>TimeZone</c>.
    /// Dates that do not specify an offset (i.e. <c>2024-01-17</c> or <c>2024-01-17T00:00:00</c>) are treated as UTC.
    /// </summary>
    /// <remarks>No DbType is set so that the provider infers the parameter type from the <see cref="DateTime.Kind"/> of the compared value. Compare against <see cref="DateTimeKind.Utc"/> values.</remarks>
    private static SqlExpression TranslatePostgreSqlCastToDate(IReadOnlyList<SqlExpression> args) {
        var textTypeMapping = new StringTypeMapping("text", null);
        var functionArgs = new SqlExpression[] {
            args[0],
            new SqlConstantExpression(PostgreSqlDateWithoutOffsetPattern, typeof(string), textTypeMapping),
            new SqlConstantExpression("\\1Z", typeof(string), textTypeMapping)
        };
        var utcDate = new SqlFunctionExpression("regexp_replace", functionArgs, nullable: true, argumentsPropagateNullability: [true, false, false], typeof(string), textTypeMapping);
        return new SqlUnaryExpression(ExpressionType.Convert, utcDate, typeof(DateTime), new PostgreSqlUtcDateTimeTypeMapping());
    }

    private static SqlExpression TranslateSqlServerCastToDate(IReadOnlyList<SqlExpression> args) {
        var datetimeOffset = new SqlFragmentExpression("datetimeoffset");
        var convertArgs = new SqlExpression[] { datetimeOffset, args[0] };
        var dateTimeOffsetTypeMapping = new DateTimeOffsetTypeMapping("datetimeoffset", dbType: null);
        var offsetDate = new SqlFunctionExpression("CONVERT", convertArgs, nullable: true, argumentsPropagateNullability: [false, true], typeof(DateTimeOffset), dateTimeOffsetTypeMapping);
        var utcDate = new SqlFunctionExpression("SWITCHOFFSET", [offsetDate, new SqlFragmentExpression("'+00:00'")], nullable: true, argumentsPropagateNullability: [true, false], typeof(DateTimeOffset), dateTimeOffsetTypeMapping);
        return new SqlFunctionExpression("CONVERT", [new SqlFragmentExpression("datetime2"), utcDate], nullable: true, argumentsPropagateNullability: [false, true], typeof(DateTime), new DateTimeTypeMapping("datetime2", dbType: null));
    }

    private sealed class PostgreSqlUtcDateTimeTypeMapping() : DateTimeTypeMapping("timestamp with time zone", dbType: null)
    {
        protected override string GenerateNonNullSqlLiteral(object value) {
            var dateTime = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
            return $"TIMESTAMPTZ '{dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture)}+00:00'";
        }
    }

    /// <summary>Matches ISO 8601 dates and date times that do not specify an offset (i.e. <c>2024-01-17</c>, <c>2024-01-17T00:00:00.000</c>).</summary>
    private const string PostgreSqlDateWithoutOffsetPattern = "^([0-9]+-[0-9]{2}-[0-9]{2}([T ][0-9]{2}:[0-9]{2}(:[0-9]{2}([.][0-9]+)?)?)?)$";

    /// <summary>
    /// Translates to <c>jsonb_extract_path_text(jsonb_build_array(jsonb_path_query_first(jsonb_path_query_first(CAST(column AS jsonb), 'strict $.path'), 'strict $ ? (@.type() != "object" &amp;&amp; @.type() != "array")')), '0')</c>.
    /// Like the SQL Server <c>JSON_VALUE</c>, returns the unquoted text of the scalar value found at the given path and SQL NULL for objects, arrays and json nulls.
    /// </summary>
    private static SqlExpression TranslatePostgreSqlJsonValue(IReadOnlyList<SqlExpression> args) {
        if (args[1] is not SqlConstantExpression { Value: string path }) {
            throw new InvalidOperationException($"The path argument of {nameof(JsonFunctions)}.{nameof(JsonFunctions.JsonValue)} must be a constant when targeting PostgreSQL.");
        }
        var textTypeMapping = new StringTypeMapping("text", null);
        var jsonbTypeMapping = new StringTypeMapping("jsonb", null);
        var segments = ParseJsonPath(path);
        SqlExpression value = new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), jsonbTypeMapping);
        if (segments.Count > 0) {
            var jsonPath = new StringBuilder("strict $");
            foreach (var segment in segments) {
                if (segment.IsArrayIndex) {
                    jsonPath.Append('[').Append(segment.Value).Append(']');
                } else {
                    jsonPath.Append('.').Append(JsonSerializer.Serialize(segment.Value));
                }
            }
            var jsonPathExpression = new SqlConstantExpression(jsonPath.ToString(), typeof(string), new StringTypeMapping("jsonpath", null));
            value = new SqlFunctionExpression("jsonb_path_query_first", [value, jsonPathExpression, new SqlFragmentExpression("'{}'::jsonb"), new SqlFragmentExpression("TRUE")], nullable: true, argumentsPropagateNullability: [true, false, false, false], typeof(string), jsonbTypeMapping);
        }
        var scalarFilter = new SqlConstantExpression("strict $ ? (@.type() != \"object\" && @.type() != \"array\")", typeof(string), new StringTypeMapping("jsonpath", null));
        var scalar = new SqlFunctionExpression("jsonb_path_query_first", [value, scalarFilter], nullable: true, argumentsPropagateNullability: [true, false], typeof(string), jsonbTypeMapping);
        var array = new SqlFunctionExpression("jsonb_build_array", [scalar], nullable: false, argumentsPropagateNullability: [false], typeof(string), jsonbTypeMapping);
        var functionArgsText = new SqlExpression[] { array, new SqlConstantExpression("0", typeof(string), textTypeMapping) };
        return new SqlFunctionExpression("jsonb_extract_path_text", functionArgsText, nullable: true, argumentsPropagateNullability: [false, false], typeof(string), textTypeMapping);
    }

    /// <summary>Splits a simple JSON path (i.e. <c>$.data.items[0]."some key"</c>) into its object keys and array indexes.</summary>
    private static List<JsonPathSegment> ParseJsonPath(string path) {
        var segments = new List<JsonPathSegment>();
        var span = path.AsSpan().Trim();
        if (span.StartsWith("$")) {
            span = span[1..];
        }
        var index = 0;
        while (index < span.Length) {
            var current = span[index];
            if (current == '.') {
                index++;
                if (index < span.Length && span[index] == '"') {
                    segments.Add(new JsonPathSegment(ReadQuoted(span, ref index, '"'), false));
                } else {
                    var start = index;
                    while (index < span.Length && span[index] != '.' && span[index] != '[') {
                        index++;
                    }
                    segments.Add(new JsonPathSegment(span[start..index].ToString(), false));
                }
            } else if (current == '[') {
                index++;
                if (index < span.Length && (span[index] == '"' || span[index] == '\'')) {
                    segments.Add(new JsonPathSegment(ReadQuoted(span, ref index, span[index]), false));
                } else {
                    var start = index;
                    while (index < span.Length && span[index] != ']') {
                        index++;
                    }
                    var arrayIndex = span[start..index].Trim().ToString();
                    if (!int.TryParse(arrayIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var indexValue)) {
                        throw new FormatException($"Invalid array index in JSON path '{path}'.");
                    }
                    segments.Add(new JsonPathSegment(indexValue.ToString(CultureInfo.InvariantCulture), true));
                }
                // skip the closing bracket.
                while (index < span.Length && span[index] != ']') {
                    index++;
                }
                index++;
            } else {
                throw new FormatException($"Invalid JSON path '{path}'.");
            }
        }
        return segments;
    }

    private readonly record struct JsonPathSegment(string Value, bool IsArrayIndex);

    private static string ReadQuoted(ReadOnlySpan<char> span, ref int index, char quote) {
        var builder = new StringBuilder();
        index++; // skip opening quote.
        while (index < span.Length && span[index] != quote) {
            if (span[index] == '\\' && index + 1 < span.Length) {
                index++;
                builder.Append(ReadEscaped(span, ref index));
                continue;
            }
            builder.Append(span[index]);
            index++;
        }
        index++; // skip closing quote.
        return builder.ToString();
    }

    /// <summary>Decodes the JSON escape sequence that starts at <paramref name="index"/> (right after the backslash).</summary>
    private static char ReadEscaped(ReadOnlySpan<char> span, ref int index) {
        var escaped = span[index++];
        switch (escaped) {
            case 'b': return '\b';
            case 'f': return '\f';
            case 'n': return '\n';
            case 'r': return '\r';
            case 't': return '\t';
            case 'u':
                if (index + 4 > span.Length || !ushort.TryParse(span.Slice(index, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code)) {
                    throw new FormatException($"Invalid unicode escape sequence in JSON path '{span}'.");
                }
                index += 4;
                return (char)code;
            default: return escaped; // i.e. \" \' \\ \/
        }
    }
}
