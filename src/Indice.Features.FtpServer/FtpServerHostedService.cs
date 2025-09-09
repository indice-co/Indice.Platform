using FubarDev.FtpServer;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.FtpServer;

/// <summary>
/// Generic host for the FTP server.
/// </summary>
public class FtpServerHostedService : IHostedService
{
    private readonly IFtpServerHost _ftpServerHost;

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpServerHostedService"/> class.
    /// </summary>
    /// <param name="ftpServerHost">The FTP server host that gets wrapped as a hosted service.</param>
    public FtpServerHostedService(
        IFtpServerHost ftpServerHost) {
        _ftpServerHost = ftpServerHost;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => _ftpServerHost.StartAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => _ftpServerHost.StopAsync(cancellationToken);
}
