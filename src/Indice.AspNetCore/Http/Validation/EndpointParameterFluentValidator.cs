using FluentValidation;
using MiniValidation;

namespace Indice.AspNetCore.Http.Validation;

internal class EndpointParameterFluentValidator : IEndpointParameterValidator
{
    private readonly IServiceProvider _serviceProvider;

    public EndpointParameterFluentValidator(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async ValueTask<(bool IsValid, IDictionary<string, string[]> Errors)> TryValidateAsync(Type argumentType, object argument) {
        var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
        var validator = _serviceProvider.GetService(validatorType) as IValidator;
        if (validator is null) {
            return await MiniValidator.TryValidateAsync(argument);
        }
        var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
        var validationContext = Activator.CreateInstance(validationContextType, argument) as IValidationContext;
        var result = await validator.ValidateAsync(validationContext);
        return new(result.IsValid, result.ToDictionary());
    }
}
