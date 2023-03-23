using MiniValidation;

namespace Indice.AspNetCore.Http.Validation;

/// <summary>
/// An abstraction over the endpoint input validation. (used to be called ModelState validation)
/// </summary>
public interface IEndpointParameterValidator
{
    /// <summary>
    /// Validates a given parameter.
    /// </summary>
    /// <param name="argument">The parameter/argument to validate</param>
    /// <returns>A dictionary of code/errors list</returns>
    public ValueTask<(bool IsValid, IDictionary<string, string[]> Errors)> TryValidateAsync(object argument);
}

internal class DefaultEndpointParameterValidator : IEndpointParameterValidator
{
    public ValueTask<(bool IsValid, IDictionary<string, string[]> Errors)> TryValidateAsync(object argument) =>
        MiniValidator.TryValidateAsync(argument);
}
