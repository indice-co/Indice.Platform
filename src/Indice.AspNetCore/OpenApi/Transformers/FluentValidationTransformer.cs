#if NET9_0_OR_GREATER
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// Provides functionality to integrate FluentValidation-based schema transformations into OpenAPI schema generation.
/// </summary>
/// <remarks>This class contains methods to enhance OpenAPI schema generation by applying validation rules defined
/// in FluentValidation. It is typically used to annotate OpenAPI schemas with constraints such as required fields,
/// length limits, and value ranges based on FluentValidation validators.</remarks>
public static class FluentValidationTransformer
{
    
    /// <summary>
    /// Adds a FluentValidation-based schema transformer to the specified OpenAPI options.
    /// </summary>
    /// <remarks>This method integrates a schema transformer that leverages FluentValidation to modify OpenAPI
    /// schemas. It is typically used to enhance schema generation by applying validation rules defined in
    /// FluentValidation.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance with the FluentValidation transformer added.</returns>
    public static OpenApiOptions AddFluentValidationTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }

    private static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        var scope = context.ApplicationServices.CreateScope();
        if (schema.Properties is not null && context.ParameterDescription is not null) {
            AnnotateSchemaPropertiesFromValidators(scope.ServiceProvider, context.JsonTypeInfo.Type, schema);
        }
        return Task.CompletedTask;
    }


    private static void AnnotateSchemaPropertiesFromValidators(IServiceProvider services, Type declaringType, OpenApiSchema schema) {
        if (declaringType is null) {
            return;
        }
        var validatorType = typeof(IValidator<>).MakeGenericType(declaringType);
        var validator = services.GetService(validatorType) as IValidator;
        if (validator is not null) {
            var descriptor = validator.CreateDescriptor();
            var validationRules = descriptor.GetMembersWithValidators();
            foreach (var validationRule in validationRules) {
                // asume camelcase
                if (string.IsNullOrWhiteSpace(validationRule.Key)) {
                    continue;
                }
                var property = validationRule.Key[..1].ToLower() + validationRule.Key[1..];
                // make sure
                property = schema.Properties.Keys.Where(x => x.Equals(property, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (property is null) {
                    continue;
                }
                foreach (IPropertyValidator propertyValidator in validationRule.Select(x => x.Validator)) {
                    switch (propertyValidator) {
                        case INotNullValidator:
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
                            if (!IsNumeric(betweenValidator.From)) {
                                break;
                            }
                            schema.Properties[property].Minimum = Convert.ToDecimal(betweenValidator.From);
                            schema.Properties[property].Maximum = Convert.ToDecimal(betweenValidator.To);
                            schema.Properties[property].ExclusiveMinimum = betweenValidator.Name.Contains("exclusive", StringComparison.OrdinalIgnoreCase);
                            schema.Properties[property].ExclusiveMaximum = betweenValidator.Name.Contains("exclusive", StringComparison.OrdinalIgnoreCase);
                            break;
                        case IComparisonValidator comparisonValidator:
                            if (!IsNumeric(comparisonValidator.ValueToCompare)) {
                                break;
                            }
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

    private static bool IsNumeric(object value) => value is not null && value.GetType().IsOneOf(typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal));

    private static bool IsOneOf(this Type type, params Type[] possibleTypes) => possibleTypes.Any(possibleType => possibleType == type);
}
#endif