using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Builds the final answer text and HTML card shown after a successful case workflow.
/// Implementations may extract data from workflow payloads using any case schema.
/// </summary>
public interface ICustomerDataCardRenderer
{
    /// <summary>
    /// Creates a presentation payload from a successful OTP validation output.
    /// </summary>
    /// <param name="input">The validated OTP workflow output.</param>
    /// <returns>The composed answer text and HTML card content.</returns>
    /// 
    /// <summary>
    /// Renders <paramref name="input"/> into a <c>text/html</c> <see cref="DataContent"/> part, using the
    /// template registered for <see cref="CustomerState.DataType"/> and falling back to a generic card.
    /// </summary>
    DataContent Render(CustomerState input);
}
