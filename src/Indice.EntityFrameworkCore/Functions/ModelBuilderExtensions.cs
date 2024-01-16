using Indice.EntityFrameworkCore.Functions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Indice.EntityFrameworkCore;

/// <summary>Extension methods for json <see cref="ModelBuilder"/></summary>
public static class ModelBuilderExtensions
{
    /// <summary>Applies configuration for all <see cref="JsonFunctions"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static ModelBuilder ApplyJsonFunctions(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        modelBuilder.ApplyJsonValue();
        modelBuilder.ApplyJsonCastToDate();
        modelBuilder.ApplyJsonCastToDouble();
#if !NET5_0_OR_GREATER // netcoreapp3.1
        modelBuilder.ApplyConvertToBoolean();
#endif
        return modelBuilder;
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.JsonValue(object, string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonValue(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        var functionBuilder = modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonValue)))
                    .HasTranslation(args => {
#if NET5_0_OR_GREATER
                        return new SqlFunctionExpression("JSON_VALUE", args, nullable: true, argumentsPropagateNullability: args.Select(x => true), typeof(string), null);
#else
                        return SqlFunctionExpression.Create("JSON_VALUE", args, typeof(string), null);
#endif
                    })
                    .HasSchema(string.Empty);
        functionBuilder.HasParameter("column").Metadata.TypeMapping = new StringTypeMapping("NVARCHAR(MAX)", null);
        return functionBuilder;
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDate(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonCastToDate(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        return modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDate)))
                    .HasTranslation(args => {
                        var datetime2 = new SqlFragmentExpression("datetime2");
                        var convertArgs = new SqlExpression[] { datetime2 }.Concat(args);
#if NET8_0_OR_GREATER
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(DateTime), null);
#else
#if NET5_0_OR_GREATER
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(DateTime?), null);                  
#else
                        return SqlFunctionExpression.Create("Convert", convertArgs, typeof(DateTime?), null);
#endif
#endif
                    })
                    .HasSchema(string.Empty);
    }

#if !NET5_0_OR_GREATER
    /// <summary>Applies configuration so that we can user <see cref="Convert.ToBoolean(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    /// <remarks>This is introduced in .net5 (missing in netcoreapp3.1 <a href="https://learn.microsoft.com/en-us/ef/core/providers/sql-server/functions">EF Sql Server Functions</a></remarks>
    public static DbFunctionBuilder ApplyConvertToBoolean(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        return modelBuilder.HasDbFunction(typeof(Convert).GetMethod(nameof(Convert.ToBoolean), new[] { typeof(string) } ))
                    .HasTranslation(args => {
                        var bit = new SqlFragmentExpression("bit");
                        var convertArgs = new SqlExpression[] { bit }.Concat(args);

                        return SqlFunctionExpression.Create("Convert", convertArgs, typeof(bool?), null);

                    })
                    .HasSchema(string.Empty);
    }
#endif

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDouble(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyJsonCastToDouble(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        return modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDouble)))
                    .HasTranslation(args => {
                        var float32 = new SqlFragmentExpression("float");
                        var convertArgs = new SqlExpression[] { float32 }.Concat(args);
#if NET5_0_OR_GREATER
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(double?), null);
#else
                        return SqlFunctionExpression.Create("Convert", convertArgs, typeof(double?), null);
#endif
                    })
                    .HasSchema(string.Empty);
    }



    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.JsonValue(object, string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyNpgJsonValue(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }

        var functionBuilder = modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonValue)))
                    .HasTranslation(args => {
#if NET5_0_OR_GREATER
                        return new SqlFunctionExpression("jsonb_path_query_first", args, nullable: true, argumentsPropagateNullability: args.Select(x => true), typeof(string), null);
#else
                        return SqlFunctionExpression.Create("jsonb_path_query_first", args, typeof(string), null);
#endif
                    })
                    .HasSchema(string.Empty);
        functionBuilder.HasParameter("column").Metadata.TypeMapping = new StringTypeMapping("text", null);
        return functionBuilder;
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDate(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyNpgJsonCastToDate(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        return modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDate)))
                    .HasTranslation(args => {
                        var datetime2 = new SqlFragmentExpression("timestampz");
                        var convertArgs = new SqlExpression[] { datetime2 }.Concat(args);
#if NET8_0_OR_GREATER
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(DateTime), null);
#else
#if NET5_0_OR_GREATER
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(DateTime?), null);                  
#else
                        return SqlFunctionExpression.Create("Convert", convertArgs, typeof(DateTime?), null);
#endif
#endif
                    })
                    .HasSchema(string.Empty);
    }

    /// <summary>Applies configuration so that we can user <see cref="JsonFunctions.CastToDouble(string)"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static DbFunctionBuilder ApplyNpgJsonCastToDouble(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        return modelBuilder.HasDbFunction(typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDouble)))
                    .HasTranslation(args => {
                        var float32 = new SqlFragmentExpression("real");
                        var convertArgs = new SqlExpression[] { float32 }.Concat(args);
#if NET5_0_OR_GREATER
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(double?), null);
#else
                        return SqlFunctionExpression.Create("Convert", convertArgs, typeof(double?), null);
#endif
                    })
                    .HasSchema(string.Empty);
    }

    /// <summary>Applies configuration for all <see cref="JsonFunctions"/></summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    public static ModelBuilder ApplyNpgJsonFunctions(this ModelBuilder modelBuilder) {
        if (modelBuilder is null) {
            throw new ArgumentNullException(nameof(modelBuilder));
        }
        modelBuilder.ApplyNpgJsonValue();
        modelBuilder.ApplyNpgJsonCastToDate();
        modelBuilder.ApplyNpgJsonCastToDouble();
        return modelBuilder;
    }
}
