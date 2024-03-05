using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Validators;
using Indice.Serialization;

/// <summary>
/// Helper methods for resolving the right type for <see cref="RuleOptionsBase"/> and <see cref="RuleOptionsBaseValidator{RuleOptionsBase}"/>.
/// </summary>
public static class ValidatorExtensions
{
    /// <summary>
    /// Deserializes the JSON body to its proper type and validates it.
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="jsonElement"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static ValidationResult ValidateDynamic(this IValidator validator, JsonElement jsonElement) {
        var validatorType = validator.GetType();
        var targetType = GetTargetTypeFromValidator(validatorType);

        if (targetType is null) {
            throw new InvalidOperationException($"Unable to determine target type from validator {validatorType.Name}");
        }

        try {
            var options = JsonSerializerOptionDefaults.GetDefaultSettings();
            var jsonObject = JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType, options);
            var validateMethod = typeof(IValidator<>).MakeGenericType(targetType).GetMethod("Validate");
            var validationResult = (ValidationResult)validateMethod.Invoke(validator, [jsonObject]);
            return validationResult;
        } catch (Exception ex) {
            throw new ArgumentException($"Unable to deserialize JsonElement to the expected type {targetType.Name}.", ex);
        }
    }

    private static Type? GetTargetTypeFromValidator(Type validatorType) {
        var validatorInterfaces = validatorType.GetInterfaces();
        var validatorInterface = validatorInterfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));
        return validatorInterface?.GenericTypeArguments.FirstOrDefault();
    }
}
