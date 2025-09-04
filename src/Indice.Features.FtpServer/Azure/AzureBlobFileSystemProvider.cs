using Azure.Storage.Blobs;
using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.FtpServer.Azure;

/// <summary>
/// Provides an implementation of <see cref="IFileSystemClassFactory"/> that creates instances of <see cref="AzureBlobFileSystem"/>.
/// </summary>
public class AzureBlobFileSystemProvider : IFileSystemClassFactory
{

    private readonly IAccountDirectoryQuery _accountDirectoryQuery;
    private readonly ILogger<AzureBlobFileSystemProvider>? _logger;

    /// <summary>
    /// Creates a new instance of <see cref="AzureBlobFileSystemProvider"/>.
    /// </summary>
    /// <param name="blobServiceClient">The azure service client</param>
    /// <param name="options">The file system oblions</param>
    /// <param name="accountDirectoryQuery">Account directory with permissions and users</param>
    /// <param name="logger">The logger</param>
    public AzureBlobFileSystemProvider(BlobServiceClient blobServiceClient,
        IOptions<AzureBlobFileSystemOptions> options,
        IAccountDirectoryQuery accountDirectoryQuery,
        ILogger<AzureBlobFileSystemProvider>? logger = null) {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        ArgumentNullException.ThrowIfNull(options);
        _accountDirectoryQuery = accountDirectoryQuery ?? throw new ArgumentNullException(nameof(accountDirectoryQuery));
        _logger = logger;
        RootPath = string.IsNullOrEmpty(options.Value.RootPath)
            ? "/"
            : Path.GetFileName(options.Value.RootPath.TrimEnd('/', '\\'));
        Container = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    /// <summary>Gets the Azure Blob Storage container client used by this file system provider.</summary>
    public BlobContainerClient Container { get; }
    
    /// <summary>Gets the root path within the blob container to use as the base directory for the FTP file system.</summary>
    public string RootPath { get; }

    /// <inheritdoc/>
    public async Task<IUnixFileSystem> Create(IAccountInformation accountInformation) {
        await Container.CreateIfNotExistsAsync();

        var path = RootPath;
        var directories = _accountDirectoryQuery.GetDirectories(accountInformation);
        if (!string.IsNullOrEmpty(directories.RootPath)) {
            path = Path.Combine(path, directories.RootPath);
        }

        _logger?.LogDebug("The root directory for {userName} is {rootPath}", accountInformation.FtpUser.Identity?.Name, path);

        var system = new AzureBlobFileSystem(Container, path);
        await system.InitAsync();
        return system;
    }
}
