using System.Globalization;
using System.Linq.Expressions;
using System.Text;
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
                functionBuilder.HasTranslation(args => {
                    var datetime2 = new SqlFragmentExpression("datetime2");
                    var convertArgs = new SqlExpression[] { datetime2 }.Concat(args);
                    return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(DateTime), null);
                });
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
        return new SqlUnaryExpression(ExpressionType.Convert, utcDate, typeof(DateTime), new DateTimeTypeMapping("timestamp with time zone", dbType: null));
    }

    /// <summary>Matches ISO 8601 dates and date times that do not specify an offset (i.e. <c>2024-01-17</c>, <c>2024-01-17T00:00:00.000</c>).</summary>
    private const string PostgreSqlDateWithoutOffsetPattern = "^([0-9]+-[0-9]{2}-[0-9]{2}([T ][0-9]{2}:[0-9]{2}(:[0-9]{2}([.][0-9]+)?)?)?)$";

    /// <summary>
    /// Translates to <c>jsonb_extract_path_text(jsonb_build_array(jsonb_path_query_first(value, 'strict $ ? (@.type() != "object" &amp;&amp; @.type() != "array")')), '0')</c>
    /// where value is <c>jsonb_extract_path(CAST(column AS jsonb), 'segment1', 'segment2', ...)</c> (or <c>CAST(column AS jsonb)</c> for the root path).
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
            var functionArgs = new List<SqlExpression> { value };
            functionArgs.AddRange(segments.Select(segment => new SqlConstantExpression(segment, typeof(string), textTypeMapping)));
            value = new SqlFunctionExpression("jsonb_extract_path", functionArgs, nullable: true, argumentsPropagateNullability: functionArgs.Select(x => true), typeof(string), jsonbTypeMapping);
        }
        var scalarFilter = new SqlConstantExpression("strict $ ? (@.type() != \"object\" && @.type() != \"array\")", typeof(string), new StringTypeMapping("jsonpath", null));
        var scalar = new SqlFunctionExpression("jsonb_path_query_first", [value, scalarFilter], nullable: true, argumentsPropagateNullability: [true, false], typeof(string), jsonbTypeMapping);
        var array = new SqlFunctionExpression("jsonb_build_array", [scalar], nullable: false, argumentsPropagateNullability: [false], typeof(string), jsonbTypeMapping);
        var functionArgsText = new SqlExpression[] { array, new SqlConstantExpression("0", typeof(string), textTypeMapping) };
        return new SqlFunctionExpression("jsonb_extract_path_text", functionArgsText, nullable: true, argumentsPropagateNullability: [false, false], typeof(string), textTypeMapping);
    }

    /// <summary>Splits a simple JSON path (i.e. <c>$.data.items[0]."some key"</c>) to its segments (i.e. <c>data</c>, <c>items</c>, <c>0</c>, <c>some key</c>).</summary>
    internal static List<string> ParseJsonPath(string path) {
        var segments = new List<string>();
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
                    segments.Add(ReadQuoted(span, ref index, '"'));
                } else {
                    var start = index;
                    while (index < span.Length && span[index] != '.' && span[index] != '[') {
                        index++;
                    }
                    segments.Add(span[start..index].ToString());
                }
            } else if (current == '[') {
                index++;
                if (index < span.Length && (span[index] == '"' || span[index] == '\'')) {
                    segments.Add(ReadQuoted(span, ref index, span[index]));
                } else {
                    var start = index;
                    while (index < span.Length && span[index] != ']') {
                        index++;
                    }
                    segments.Add(span[start..index].Trim().ToString());
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
                    throw new FormatException($"Invalid unicode escape sequence in JSON path '{span.ToString()}'.");
                }
                index += 4;
                return (char)code;
            default: return escaped; // i.e. \" \' \\ \/
        }
    }
}
