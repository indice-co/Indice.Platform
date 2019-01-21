using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Adds all derived types of any given <typeparamref name="TBaseType"/>
    /// </summary>
    /// <typeparam name="TBaseType"></typeparam>
    public class PolymorphicSchemaFilter<TBaseType> : ISchemaFilter
    {
        /// <summary>
        /// Derived types of <typeparamref name="TBaseType"/>
        /// </summary>
        public List<Type> DerivedTypes { get; }

        /// <summary>
        /// Derived types of <typeparamref name="TBaseType"/>
        /// </summary>
        public Dictionary<string, string> DiscriminatorMap { get; }

        /// <summary>
        /// The property name used to determine the type of this object
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Construcs the schema filter by searching for an Enum which values match the type names.
        /// </summary>
        public PolymorphicSchemaFilter() : this(null, null) {

        }

        /// <summary>
        /// Construcs the schema filter
        /// </summary>
        public PolymorphicSchemaFilter(string discriminator, Dictionary<string, string> map) {
            var baseType = typeof(TBaseType);
            DerivedTypes = baseType.Assembly
                            .GetTypes()
                            .Where(t =>
                                t != baseType &&
                                baseType.IsAssignableFrom(t)
                                ).ToList();

            if (string.IsNullOrEmpty(discriminator)) {
                var candidate = baseType.GetProperties().Where(x => x.PropertyType.IsEnum).FirstOrDefault() ??
                                baseType.GetProperties().Where(x => x.PropertyType == typeof(string) && x.Name.IndexOf("type", StringComparison.OrdinalIgnoreCase) > -1).FirstOrDefault() ??
                                throw new ArgumentNullException(nameof(discriminator));

                discriminator = candidate.Name.Camelize();
                if (candidate.PropertyType.IsEnum && map == null) {
                    var enumNames = Enum.GetNames(candidate.PropertyType);
                    map = DerivedTypes.Select(x => new KeyValuePair<string, string>(x.Name, ResolveDiscriminatorValue(x, candidate, enumNames)))
                                                   .ToDictionary(x => x.Value, x => new OpenApiReference { Type = ReferenceType.Schema, Id = x.Key }.ReferenceV3);
                }

            }
            Discriminator = discriminator;
            DiscriminatorMap = map;
        }

        private string ResolveDiscriminatorValue(Type type, PropertyInfo discriminator, string[] options) {
            var value = type.Name;
            try {
                value = options.Where(name => type.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) > -1).Single();
            } catch {
                value = discriminator.GetValue(Activator.CreateInstance(type), null).ToString();
            }
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (DerivedTypes.Count == 0) {
                return;
            }
            if (typeof(TBaseType) == context.SystemType) { // when it is the base type
                //context.SchemaRegistry.Schemas.ContainsKey(enumType.Name)
                schema.Discriminator = new OpenApiDiscriminator { PropertyName = Discriminator, Mapping = DiscriminatorMap };
                if (!context.SchemaRegistry.Schemas.ContainsKey(DerivedTypes[0].Name)) {
                    foreach (var type in DerivedTypes) {
                        var derivedSchema = context.SchemaRegistry.GetOrRegister(type);
                        SubclassSchema(schema, derivedSchema, type, context);
                    }
                }
            } else if (DerivedTypes.Contains(context.SystemType)) { // when it is the derived type
                if (schema.AllOf?.Count == 0) { // and it is not altered to indicate inheritance.
                    var baseSchema = context.SchemaRegistry.GetOrRegister(typeof(TBaseType));
                    baseSchema.Discriminator = baseSchema.Discriminator ?? new OpenApiDiscriminator { PropertyName = Discriminator, Mapping = DiscriminatorMap };
                    SubclassSchema(baseSchema, schema, context.SystemType, context);
                }
            } else {
                var baseTypeProperties = schema.Properties
                                      .Where(p => p.Value.Reference?.Id == typeof(TBaseType).Name)
                                      .Union(schema.Properties.Where(p => p.Value.Items?.Reference?.Id == typeof(TBaseType).Name))
                                      .ToList();

                foreach (var prop in baseTypeProperties) {
                    if (prop.Value.Reference?.Id == typeof(TBaseType).Name) {
                        prop.Value.Reference = null;
                        prop.Value.OneOf = DerivedTypes.Select(x =>
                            new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = x.Name } }
                        ).ToList();
                    } else {
                        prop.Value.Items.Reference = null;
                        prop.Value.Items.AnyOf = DerivedTypes.Select(x =>
                            new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = x.Name } }
                        ).ToList();
                    }
                }
            }
        }

        private static void SubclassSchema(OpenApiSchema baseSchema, OpenApiSchema derivedSchema, Type derivedType, SchemaFilterContext context) {
            var extraProps = derivedSchema.Properties.Where(x => !baseSchema.Properties.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            var extraRequired = derivedSchema.Required.Where(x => !baseSchema.Properties.ContainsKey(x));
            context.SchemaRegistry.Schemas[derivedType.Name] = new OpenApiSchema {
                AllOf = new List<OpenApiSchema> {
                                new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = context.SystemType.Name } },
                                new OpenApiSchema {
                                    Type = "object",
                                    Properties = extraProps,
                                    Required = new HashSet<string>(extraRequired)
                                }
                            }
            };
        }

    }
}
