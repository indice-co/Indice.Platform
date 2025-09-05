using FubarDev.FtpServer.FileSystem;
using Indice.Features.FtpServer.Azure;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace FubarDev.FtpServer;

/// <summary>
/// Extension methods for <see cref="IFtpServerBuilder"/>.
/// </summary>
public static class AzureBlobExtensions
{
    /// <summary>
    /// Uses the Azure blob storage file system API.
    /// </summary>
    /// <param name="builder">The server builder used to configure the FTP server.</param>
    /// <param name="configureAction">An optional action to configure the <see cref="AzureBlobFileSystemOptions"/>.</param>
    /// <returns>the server builder used to configure the FTP server.</returns>
    public static IFtpServerBuilder UseAzureBlobFileSystem(this IFtpServerBuilder builder, Action<AzureBlobFileSystemOptions>? configureAction = null) {
        if (configureAction is not null) {
            builder.Services.Configure(configureAction);
            var options = new AzureBlobFileSystemOptions();
            configureAction?.Invoke(options);
            if (!string.IsNullOrWhiteSpace(options.ConnectrionString)) {
                //https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection?tabs=web-app-builder
                builder.Services.AddAzureClients(clientBuilder => {
                    clientBuilder.AddBlobServiceClient(options.ConnectrionString);
                });
            }
        }
        builder.Services.AddSingleton<IFileSystemClassFactory, AzureBlobFileSystemProvider>();
        return builder;
    }
}
