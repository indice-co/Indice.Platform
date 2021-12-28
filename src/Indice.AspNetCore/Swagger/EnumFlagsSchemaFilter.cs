using System;
using System.Linq;
using Indice.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// 
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
            if (schema.Items is null) {
                schema.Items = new OpenApiSchema();
            }
            schema.Items.Enum = Enum.GetNames(context.Type).Select(name => (IOpenApiAny)new OpenApiString(name)).ToList();
            schema.Enum = null;
        }
    }
}
