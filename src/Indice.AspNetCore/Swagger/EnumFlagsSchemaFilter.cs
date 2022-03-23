using System;
using System.Linq;
using Indice.Extensions;
using Indice.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Changes the OAS for enum flags and treats them as an array. This works in accordance with serialization by using the <see cref="JsonStringArrayEnumFlagsConverterFactory"/>.
    /// </summary>
    public class EnumFlagsSchemaFilter : ISchemaFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (!context.Type.IsFlagsEnum()) {
                return;
            }
            schema.Type = "array";
            schema.Format = null;
            schema.Nullable = context.Type.IsReferenceOrNullableType();
            schema.Enum = null;
            schema.Items ??= new OpenApiSchema();
            schema.Items.Type = "string";
            schema.Items.Enum = Enum.GetNames(context.Type).Select(name => (IOpenApiAny)new OpenApiString(name)).ToList();
        }
    }
}
