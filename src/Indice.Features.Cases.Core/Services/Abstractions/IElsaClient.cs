namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>Contract for sending external workflow messages for cases.</summary>
public interface IElsaClient
{
    /// <summary>Sends an external message payload to the workflow endpoint for the specified workflow instance.</summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendExternalMessage(Guid workflowId, object request, CancellationToken cancellationToken = default);

    /// <summary>Sends an external message payload to a named workflow endpoint for the specified workflow instance.</summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <param name="endpointName">The target endpoint name.</param>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendExternalMessage(Guid workflowId, string? endpointName, object request, CancellationToken cancellationToken = default);
}
