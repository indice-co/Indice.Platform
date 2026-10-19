namespace Indice.Features.Agents.Core.Services.Validation;

/// <summary>Validates the user's challenge reply against the expected case value.</summary>
public interface IChallengeValidator
{
    /// <summary>Determines whether the user input matches the expected value.</summary>
    /// <param name="userInput">The value provided by the user.</param>
    /// <param name="expectedValue">The actual value stored in the case.</param>
    /// <returns><see langword="true"/> if the input is valid; otherwise <see langword="false"/>.</returns>
    bool IsValid(string userInput, string expectedValue);
}
