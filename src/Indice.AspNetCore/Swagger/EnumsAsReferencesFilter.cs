using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Filter that handles enum types as references. This results to only one enum generated when it is referenced by multiple classes of interfaces.
    /// </summary>
    public class EnumsAsReferencesFilter : ISchemaFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (schema.Properties == null) {
                return;
            }

            var enumProperties = schema.Properties
                                      .Where(p => p.Value.Enum?.Count > 0)
                                      .Union(schema.Properties.Where(p => p.Value.Items?.Enum.Count > 0))
                                      .ToList();

            var enums = context.SystemType
                               .GetProperties()
                               .Select(p => new KeyValuePair<string, Type>(
                                                    p.Name,
                                                    Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType.GetElementType() ?? p.PropertyType.GetGenericArguments().FirstOrDefault() ?? p.PropertyType))
                               .Where(p => p.Value.GetTypeInfo().IsEnum)
                               .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var enumProperty in enumProperties) {
                var enumPropertyValue = enumProperty.Value.Enum?.Count > 0 ? enumProperty.Value : enumProperty.Value.Items;
                enums.TryGetValue(enumProperty.Key, out var enumType);
                if (enumType == null) {
                    throw new Exception($"Property {enumProperty} not found in {context.SystemType.Name} Type.");
                }

                var enumSchema = context.SchemaRegistry.GetOrRegister(enumType);
                if (!context.SchemaRegistry.Schemas.ContainsKey(enumType.Name)) {
                    enumSchema.Extensions.Add(
                        "x-ms-enum",
                        new OpenApiObject {
                            ["name"] = new OpenApiString(enumType.Name),
                            ["modelAsString"] = new OpenApiBoolean(true)
                        }
                    );
                    context.SchemaRegistry.Schemas.Add(enumType.Name, enumSchema);
                }

                var valueSchema = new OpenApiSchema {
                    Reference = new OpenApiReference {
                        Id = enumType.Name,
                        Type = ReferenceType.Schema
                    },
                    Nullable = enumPropertyValue.Nullable,
                    Required = enumPropertyValue.Required,
                    ReadOnly = enumPropertyValue.ReadOnly,
                };

                if (enumProperty.Value.Enum?.Count > 0) {
                    schema.Properties[enumProperty.Key] = valueSchema;
                } else if (enumProperty.Value.Items?.Enum != null) {
                    schema.Properties[enumProperty.Key].Items = valueSchema;
                }
            }
        }
    }
}
