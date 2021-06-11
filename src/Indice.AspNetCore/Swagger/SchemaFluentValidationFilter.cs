using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Filter that finds required validators searching in FluentValidation.
    /// </summary>
    public class SchemaFluentValidationFilter : ISchemaFilter
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new instance of <see cref="SchemaFluentValidationFilter"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        public SchemaFluentValidationFilter(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (context.Type == typeof(void)) {
                return;
            }
            IValidator validator;
            using (var scope = _serviceProvider.CreateScope()) {
                var validatorFactory = scope.ServiceProvider.GetService<IValidatorFactory>();
                validator = validatorFactory?.GetValidator(context.Type);
            }
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
            validators.Select(x => x.Item1).OfType<INotNullValidator>()
                      .Cast<IPropertyValidator>()
                      .Concat(validators.Select(x => x.Item1).OfType<INotEmptyValidator>()
                      .Cast<IPropertyValidator>())
                      .Count() > 0;
    }
}
