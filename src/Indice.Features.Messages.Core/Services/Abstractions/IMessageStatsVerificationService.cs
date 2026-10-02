namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>
/// Interface for the service that verifies the send statistics.
/// </summary>
public interface IMessageStatsVerificationService
{
    /// <summary>
    /// Recounts the successful sends of the last complete UTC days from the saved message events and corrects the send statistics.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task VerifyAsync(CancellationToken cancellationToken = default);
}
