using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;
using Humanizer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// filter that finds required validators searching in FluentValidation
    /// </summary>
    public class SchemaFluentValidationFilter : ISchemaFilter
    {
        private readonly IValidatorFactory _factory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory"></param>
        public SchemaFluentValidationFilter(IValidatorFactory factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            var validator = _factory.GetValidator(context.Type);

            if (validator != null && schema.Properties != null) {
                foreach (var item in schema.Properties) {
                    var validators = validator.CreateDescriptor().GetValidatorsForMember(item.Key.Pascalize());

                    if (IsRequired(validators)) {
                        schema.Required = schema.Required ?? new HashSet<string>();
                        schema.Required.Add(item.Key);
                    }
                    var patterns = validators.OfType<IRegularExpressionValidator>();
                    //if (patterns.Any()) {
                    //    schema.Pattern =  
                    //}
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
