using System;
using System.Linq;
using System.Reflection;
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
        /// <param name="model"></param>
        /// <param name="context"></param>
        public void Apply(Schema model, SchemaFilterContext context) {
            if (model.Properties == null) {
                return;
            }

            var enumProperties = model.Properties
                                      .Where(p => p.Value.Enum != null)
                                      .Union(model.Properties.Where(p => p.Value.Items?.Enum != null))
                                      .ToList();

            var enums = context.SystemType
                               .GetProperties()
                               .Select(p => Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType.GetElementType() ?? p.PropertyType.GetGenericArguments().FirstOrDefault() ?? p.PropertyType)
                               .Where(p => p.GetTypeInfo().IsEnum)
                               .ToList();

            foreach (var enumProperty in enumProperties) {
                var enumPropertyValue = enumProperty.Value.Enum != null ? enumProperty.Value : enumProperty.Value.Items;
                var enumValues = enumPropertyValue.Enum.Select(e => $"{e}").ToList();

                var enumType = enums.SingleOrDefault(type => {
                    var enumNames = Enum.GetNames(type);

                    if (enumNames.Except(enumValues, StringComparer.InvariantCultureIgnoreCase).Any()) {
                        return false;
                    }

                    if (enumValues.Except(enumNames, StringComparer.InvariantCultureIgnoreCase).Any()) {
                        return false;
                    }

                    return true;
                });

                if (enumType == null) {
                    throw new Exception($"Property {enumProperty} not found in {context.SystemType.Name} Type.");
                }

                if (context.SchemaRegistry.Definitions.ContainsKey(enumType.Name) == false) {
                    context.SchemaRegistry.Definitions.Add(enumType.Name, enumPropertyValue);
                }

                var schema = new Schema {
                    Ref = $"#/definitions/{enumType.Name}"
                };

                if (enumProperty.Value.Enum != null) {
                    model.Properties[enumProperty.Key] = schema;
                } else if (enumProperty.Value.Items?.Enum != null) {
                    enumProperty.Value.Items = schema;
                }
            }
        }
    }
}
