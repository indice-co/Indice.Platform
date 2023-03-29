#if NET7_0_OR_GREATER
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
    /// <param name="argumentType">The type to validate</param>
    /// <param name="argument">The parameter/argument to validate</param>
    /// <returns>A dictionary of code/errors list</returns>
    public ValueTask<(bool IsValid, IDictionary<string, string[]> Errors)> TryValidateAsync(Type argumentType, object argument);
}

internal class DefaultEndpointParameterValidator : IEndpointParameterValidator
{
    public ValueTask<(bool IsValid, IDictionary<string, string[]> Errors)> TryValidateAsync(Type argumentType, object argument) =>
        MiniValidator.TryValidateAsync(argument);
}
#endif