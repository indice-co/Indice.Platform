using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Ports;

/// <summary>
/// Represents a request port for operation selection in the workflow.
/// </summary>
public static class OperationRequestPort
{
    /// <summary>
    /// Represents an operation request to allow the user to choose between available options.
    /// </summary>
    /// <param name="SupportedOperations">The operations the user can choose from.</param>
    public record OperationRequest(List<string> SupportedOperations);

    /// <summary>
    /// Represents a response containing the operation the user want to perform.
    /// </summary>
    /// <param name="SelectedOperation">The selected operation.</param>
    public record OperationResponse(string SelectedOperation);

    /// <summary>
    /// Creates a request port for operation selection in the workflow.
    /// </summary>
    /// <param name="id">The identifier for the request port.</param>
    /// <returns>A request port for OTP verification.</returns>
    public static RequestPort<OperationRequest, OperationResponse> Create(string id = nameof(OperationRequest)) => RequestPort.Create<OperationRequest, OperationResponse>(id);
}
