using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Humanizer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Filter that finds required validators searching in FluentValidation.
    /// </summary>
    public class SchemaFluentValidationFilter : ISchemaFilter
    {
        private readonly IValidatorFactory _factory;

        /// <summary>
        /// Creates a new instance of <see cref="SchemaFluentValidationFilter"/>.
        /// </summary>
        /// <param name="factory">Gets validators for a particular type.</param>
        public SchemaFluentValidationFilter(IValidatorFactory factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        /// <inheritdoc />
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (context.Type == typeof(void)) {
                return;
            }
            var validator = _factory.GetValidator(context.Type);
            if (validator != null && schema.Properties != null) {
                foreach (var item in schema.Properties) {
                    var validators = validator.CreateDescriptor().GetValidatorsForMember(item.Key.Pascalize());
                    if (IsRequired(validators)) {
                        schema.Required ??= new HashSet<string>();
                        schema.Required.Add(item.Key);
                    }
                }
            }
        }

        private bool IsRequired(IEnumerable<(IPropertyValidator, IRuleComponent)> validators) =>
            validators.OfType<INotNullValidator>()
                      .Cast<IPropertyValidator>()
                      .Concat(validators.OfType<INotEmptyValidator>()
                      .Cast<IPropertyValidator>())
                      .Count() > 0;
    }
}
