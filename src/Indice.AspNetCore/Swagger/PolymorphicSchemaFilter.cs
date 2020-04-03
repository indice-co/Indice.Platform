using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Adds all derived types of any given <typeparamref name="TBaseType"/>
    /// </summary>
    /// <typeparam name="TBaseType"></typeparam>
    public class PolymorphicSchemaFilter<TBaseType> : PolymorphicSchemaFilter
    {
        /// <summary>
        /// Construcs the schema filter.
        /// </summary>
        public PolymorphicSchemaFilter() : this(null, null) { }

        /// <summary>
        /// Construcs the schema filter.
        /// </summary>
        /// <param name="discriminator">The property that will be used or added to the schema as the Type discriminator</param>
        /// <param name="map">A dictionary that provides the value to Type name</param>
        public PolymorphicSchemaFilter(string discriminator, Dictionary<string, Type> map) : base(typeof(TBaseType), discriminator, map) { }
    }

    /// <summary>
    /// Adds all derived types of any given base type.
    /// </summary>
    public class PolymorphicSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// The base type.
        /// </summary>
        public Type BaseType { get; }
        /// <summary>
        /// Derived types of <see cref="BaseType"/>
        /// </summary>
        public List<Type> DerivedTypes { get; }
        /// <summary>
        /// Derived types of <see cref="BaseType"/>
        /// </summary>
        public IDictionary<string, string> DiscriminatorMap { get; }
        /// <summary>
        /// AnyOf AllOf List for <see cref="BaseType"/>
        /// </summary>
        public IList<OpenApiSchema> AllOfReferences { get; }
        /// <summary>
        /// The property name used to determine the type of this object
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Construcs the schema filter
        /// </summary>
        /// <param name="baseType">The base type</param>
        /// <param name="discriminator">The property that will be used or added to the schema as the Type discriminator.</param>
        /// <param name="map">A dictionary that provides the value to <see cref="Type"/> name.</param>
        public PolymorphicSchemaFilter(Type baseType, string discriminator, IDictionary<string, Type> map) {
            BaseType = baseType;
            if (discriminator == null) {
                discriminator = baseType.GetRuntimeProperties().Where(x => x.PropertyType.IsEnum).FirstOrDefault()?.Name;
            }
            discriminator ??= "discriminator";
            map ??= JsonPolymorphicConverter.GetTypeMapping(baseType, discriminator);
            DiscriminatorMap = map.ToDictionary(x => x.Key, x => new OpenApiReference { 
                Type = ReferenceType.Schema, 
                Id = x.Value.Name 
            }.ReferenceV3);
            DerivedTypes = map.Values.Where(x => x != baseType).ToList();
            AllOfReferences = map.Values.Where(x => !x.IsAbstract).Select(x => new OpenApiSchema { 
                Reference = new OpenApiReference { 
                    Type = ReferenceType.Schema, 
                    Id = x.Name 
                } 
            }).ToList();
            Discriminator = discriminator;
        }

        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (DerivedTypes.Count == 0) {
                return;
            }
            if (BaseType == context.Type) { // when it is the base type
                schema.Discriminator = new OpenApiDiscriminator { PropertyName = Discriminator, Mapping = DiscriminatorMap };
                foreach (var type in DerivedTypes.Where(x => x.Name != BaseType.Name && !context.SchemaRepository.Schemas.ContainsKey(x.Name))) {
                    var derivedSchema = default(OpenApiSchema);
                    if (!context.SchemaRepository.TryGetIdFor(type, out var derivedSchemaId)) {
                        derivedSchema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                    }
                    if (context.SchemaRepository.Schemas.ContainsKey(derivedSchema.Reference?.Id ?? type.Name)) {
                        derivedSchema = context.SchemaRepository.Schemas[derivedSchema.Reference?.Id ?? type.Name];
                    } else {
                        derivedSchema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = type.Name } };
                    }
                    SubclassSchema(schema, derivedSchema, type, context);
                }
            } else if (!DerivedTypes.Contains(context.Type)) { // When it is neither the derived type or base type.
                var baseTypeProperties = schema.Properties
                                               .Where(p => p.Value.Reference?.Id == BaseType.Name)
                                               .Union(schema.Properties.Where(p => p.Value.Items?.Reference?.Id == BaseType.Name))
                                               .ToList();
                foreach (var prop in baseTypeProperties) {
                    if (prop.Value.Reference?.Id == BaseType.Name) {
                        prop.Value.Reference = null;
                        prop.Value.OneOf = AllOfReferences;
                    } else {
                        prop.Value.Items.Reference = null;
                        prop.Value.Items.AnyOf = AllOfReferences;
                    }
                }
            }
        }

        private static void SubclassSchema(OpenApiSchema baseSchema, OpenApiSchema derivedSchema, Type derivedType, SchemaFilterContext context) {
            var extraProps = derivedSchema.Properties.Where(x => !baseSchema.Properties.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            var extraRequired = derivedSchema.Required.Where(x => !baseSchema.Properties.ContainsKey(x));
            context.SchemaRepository.Schemas[derivedType.Name] = new OpenApiSchema {
                AllOf = new List<OpenApiSchema> {
                    new OpenApiSchema {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.Schema,
                            Id = context.Type.Name
                        }
                    },
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
