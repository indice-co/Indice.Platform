using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Ports;

/// <summary>
/// Represents a request port for operation selection in the workflow.
/// </summary>
public static class UserInputRequestPort
{
    /// <summary>
    /// Represents an operation request to allow the user to choose between available options.
    /// </summary>
    /// <param name="Message">The operations the user can choose from.</param>
    public record UserInputRequest(string Message);

    /// <summary>
    /// Represents a response containing the operation the user want to perform.
    /// </summary>
    /// <param name="InputMessage">The selected operation.</param>
    public record UserResponse(string InputMessage);

    /// <summary>
    /// Creates a request port for operation selection in the workflow.
    /// </summary>
    /// <param name="id">The identifier for the request port.</param>
    /// <returns>A request port for OTP verification.</returns>
    public static RequestPort<UserInputRequest, UserResponse> Create(string id = nameof(UserInputRequest)) => RequestPort.Create<UserInputRequest, UserResponse>(id);
}
