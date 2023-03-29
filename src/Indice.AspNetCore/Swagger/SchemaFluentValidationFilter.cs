using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger;

/// <summary>Filter that finds required validators searching in FluentValidation.</summary>
public class SchemaFluentValidationFilter : ISchemaFilter
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="SchemaFluentValidationFilter"/>.</summary>
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
            validator = scope.ServiceProvider.GetService(context.Type) as IValidator;
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

/// <summary>Filter that decorates request parameters with validators by searching in FluentValidation.</summary>
public class RequestBodyFluentValidationSwaggerFilter : IRequestBodyFilter
{
    private readonly IServiceProvider services;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="services"></param>
    public RequestBodyFluentValidationSwaggerFilter(IServiceProvider services) {
        this.services = services;
    }
    /// <inheritdoc/>
    public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context) {
        Type validatorType = typeof(IValidator<>).MakeGenericType(context.BodyParameterDescription.Type);
        IValidator? validator = services.GetService(validatorType) as IValidator;
        if (validator is not null) {
            OpenApiSchema schema = context.SchemaRepository.Schemas[context.BodyParameterDescription.Type.FullName];
            IValidatorDescriptor descriptor = validator.CreateDescriptor();
            ILookup<string, (IPropertyValidator Validator, IRuleComponent Options)> validationRules = descriptor.GetMembersWithValidators();
            foreach (IGrouping<string, (IPropertyValidator Validator, IRuleComponent Options)> validationRule in validationRules) {
                string property = validationRule.Key[..1].ToLower() + validationRule.Key[1..];
                foreach (IPropertyValidator propertyValidator in validationRule.Select(x => x.Validator)) {
                    switch (propertyValidator) {
                        case INotEmptyValidator:
                            schema.Properties[property].Nullable = false;
                            break;
                        case IMinimumLengthValidator minLengthValidator:
                            schema.Properties[property].MinLength = minLengthValidator.Min;
                            break;
                        case IMaximumLengthValidator maxLengthValidator:
                            schema.Properties[property].MaxLength = maxLengthValidator.Max;
                            break;
                        case ILengthValidator lengthValidator:
                            schema.Properties[property].MinLength = lengthValidator.Min;
                            schema.Properties[property].MaxLength = lengthValidator.Max;
                            break;
                        case IBetweenValidator betweenValidator:
                            schema.Properties[property].Minimum = Convert.ToDecimal(betweenValidator.From);
                            schema.Properties[property].Maximum = Convert.ToDecimal(betweenValidator.To);
                            schema.Properties[property].ExclusiveMinimum = betweenValidator.Name.Contains("exclusive", StringComparison.OrdinalIgnoreCase);
                            schema.Properties[property].ExclusiveMaximum = betweenValidator.Name.Contains("exclusive", StringComparison.OrdinalIgnoreCase);
                            break;
                        case IComparisonValidator comparisonValidator:
                            if (comparisonValidator.Comparison == Comparison.LessThan) {
                                schema.Properties[property].Maximum = Convert.ToDecimal(comparisonValidator.ValueToCompare);
                                schema.Properties[property].ExclusiveMaximum = true;
                            } else if (comparisonValidator.Comparison == Comparison.LessThanOrEqual) {
                                schema.Properties[property].Maximum = Convert.ToDecimal(comparisonValidator.ValueToCompare);
                                schema.Properties[property].ExclusiveMaximum = false;
                            } else if (comparisonValidator.Comparison == Comparison.GreaterThan) {
                                schema.Properties[property].Minimum = Convert.ToDecimal(comparisonValidator.ValueToCompare);
                                schema.Properties[property].ExclusiveMaximum = true;
                            } else if (comparisonValidator.Comparison == Comparison.GreaterThanOrEqual) {
                                schema.Properties[property].Minimum = Convert.ToDecimal(comparisonValidator.ValueToCompare);
                                schema.Properties[property].ExclusiveMaximum = false;
                            }
                            break;
                    }
                }
            }
        }
    }
}