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
                        return new SqlFunctionExpression("JSON_VALUE", args, nullable: true, argumentsPropagateNullability: args.Select(x => true), typeof(string), null);
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
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(DateTime), null);
                    })
                    .HasSchema(string.Empty);
    }

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
                        return new SqlFunctionExpression("Convert", convertArgs, nullable: true, argumentsPropagateNullability: convertArgs.Select(x => true), typeof(double?), null);
                    })
                    .HasSchema(string.Empty);
    }
}
