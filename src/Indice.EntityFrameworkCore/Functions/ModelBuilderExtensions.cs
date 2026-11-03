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
                // No DbType so that the provider infers the parameter type (timestamp or timestamptz) from the DateTime.Kind of the compared value.
                functionBuilder.HasTranslation(args => new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(DateTime), new DateTimeTypeMapping("timestamp", dbType: null)));
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

    /// <summary>Translates to <c>jsonb_extract_path_text(CAST(column AS jsonb), 'segment1', 'segment2', ...)</c> which, like the SQL Server <c>JSON_VALUE</c>, returns the unquoted text of the value found at the given path.</summary>
    private static SqlExpression TranslatePostgreSqlJsonValue(IReadOnlyList<SqlExpression> args) {
        if (args[1] is not SqlConstantExpression { Value: string path }) {
            throw new InvalidOperationException($"The path argument of {nameof(JsonFunctions)}.{nameof(JsonFunctions.JsonValue)} must be a constant when targeting PostgreSQL.");
        }
        var textTypeMapping = new StringTypeMapping("text", null);
        var segments = ParseJsonPath(path);
        if (segments.Count == 0) {
            return new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), textTypeMapping);
        }
        var functionArgs = new List<SqlExpression> {
            new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), new StringTypeMapping("jsonb", null))
        };
        functionArgs.AddRange(segments.Select(segment => new SqlConstantExpression(segment, typeof(string), textTypeMapping)));
        return new SqlFunctionExpression("jsonb_extract_path_text", functionArgs, nullable: true, argumentsPropagateNullability: functionArgs.Select(x => true), typeof(string), textTypeMapping);
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
            }
            builder.Append(span[index]);
            index++;
        }
        index++; // skip closing quote.
        return builder.ToString();
    }
}
