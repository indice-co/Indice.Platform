namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Builds the final answer text and HTML card shown after a successful case workflow.
/// Implementations may extract data from workflow payloads using any case schema.
/// </summary>
public interface ICasePresentationFormatter
{
    /// <summary>
    /// Creates a presentation payload from a successful OTP validation output.
    /// </summary>
    /// <param name="input">The validated OTP workflow output.</param>
    /// <returns>The composed answer text and HTML card content.</returns>
    CasePresentationResult Format(OtpValidationOutput input);
}

/// <summary>
/// Presentation result containing the plain text answer and the rendered HTML card.
/// </summary>
/// <param name="Answer">The plain text answer returned by the workflow.</param>
/// <param name="HtmlCard">The HTML card payload rendered for the client.</param>
public sealed record CasePresentationResult(string Answer, string HtmlCard);
