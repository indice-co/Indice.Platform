using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Ports;

/// <summary>
/// Represents a request port for Ownership verification in the workflow.
/// </summary>
public static class ChallengeRequestPort
{
    /// <summary>
    /// Represents a request for an verfication request
    /// </summary>
    /// <param name="message">The message for the user.</param>
    public record ChallengeRequest(string message);

    /// <summary>
    /// Represents a response for an verfication request containing users input
    /// </summary>
    /// <param name="userInput">Users input.</param>
    public record ChallengeResponse(string userInput);

    /// <summary>
    /// Creates a request port for OTP (One-Time Password) verification in the workflow.
    /// </summary>
    /// <param name="id">The identifier for the request port.</param>
    /// <returns>A request port for OTP verification.</returns>
    public static RequestPort<ChallengeRequest, ChallengeResponse> Create(string id = nameof(ChallengeRequestPort)) => RequestPort.Create<ChallengeRequest, ChallengeResponse>(id);
}