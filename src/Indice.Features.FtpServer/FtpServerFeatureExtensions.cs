using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem.DotNet;
using Indice.Features.FtpServer;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IFtpServerBuilder"/>.
/// </summary>
public static class FtpServerFeatureExtensions
{
    /// <summary>
    /// Runs the FTP server as a hosted service.
    /// </summary>
    /// <param name="builder">The server builder used to configure the FTP server.</param>
    /// <param name="configureAction">The configure action</param>
    /// <returns>The builder for further configuration</returns>
    public static IFtpServerBuilder RunAsHostedService(this IFtpServerBuilder builder, Action<FtpServerOptions>? configureAction = null) {
        if (configureAction is not null) {
            builder.Services.Configure(configureAction);
        }
        builder.Services.AddHostedService<FtpServerHostedService>();
        return builder;
    }

    /// <summary>
    /// Uses the .NET file system API.
    /// </summary>
    /// <param name="builder">The server builder used to configure the FTP server.</param>
    /// <param name="configureAction">The configure action</param>
    /// <returns>The builder for further configuration</returns>
    /// <returns></returns>
    public static IFtpServerBuilder UseDotNetFileSystem(this IFtpServerBuilder builder, Action<DotNetFileSystemOptions> configureAction) {
        builder.Services.Configure(configureAction);
        builder.UseDotNetFileSystem();
        return builder;
    }
}
