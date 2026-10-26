using System.ComponentModel.DataAnnotations;
using FubarDev.FtpServer;
using FubarDev.FtpServer.DataConnection;
using FubarDev.FtpServer.Features;

namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// A no-op data connection validator that is used as a hook in order to keep track of the data connections that get established.
/// </summary>
internal sealed class TrackingDataConnectionValidator : IFtpDataConnectionValidator
{
    private readonly FtpServerConnectionTracker _tracker;

    public TrackingDataConnectionValidator(FtpServerConnectionTracker tracker) {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    public Task<ValidationResult?> ValidateAsync(IFtpConnection connection, IFtpDataConnectionFeature dataConnectionFeature, IFtpDataConnection dataConnection, CancellationToken cancellationToken) {
        _tracker.DataConnectionOpened(dataConnection);
        return Task.FromResult<ValidationResult?>(ValidationResult.Success);
    }
}
