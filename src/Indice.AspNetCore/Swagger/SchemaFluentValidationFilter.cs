using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;
using Humanizer;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    internal class SchemaFluentValidationFilter : ISchemaFilter
    {
        private readonly IValidatorFactory _factory;

        public SchemaFluentValidationFilter(IValidatorFactory factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public void Apply(Schema model, SchemaFilterContext context) {
            var validator = _factory.GetValidator(context.SystemType);

            if (validator != null) {
                foreach (var item in model.Properties) {
                    var validators = validator.CreateDescriptor().GetValidatorsForMember(item.Key.Pascalize());

                    if (IsRequired(validators)) {
                        model.Required = model.Required ?? new List<string>();
                        model.Required.Add(item.Key);
                    }
                }
            }
        }

        bool IsRequired(IEnumerable<IPropertyValidator> validators) {
            var notEmpty = false;

            notEmpty = validators.OfType<INotNullValidator>()
                                 .Cast<IPropertyValidator>()
                                 .Concat(validators.OfType<INotEmptyValidator>()
                                 .Cast<IPropertyValidator>())
                                 .Count() > 0;

            return notEmpty;
        }
    }
}
