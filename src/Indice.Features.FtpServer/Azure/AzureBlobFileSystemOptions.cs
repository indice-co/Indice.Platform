namespace Indice.Features.FtpServer.Azure;

/// <summary>
/// Options for configuring the Azure Blob Storage file system for the FTP server.
/// </summary>
public class AzureBlobFileSystemOptions
{
    /// <summary>The connection string used to connect to the Azure Blob Storage account.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>The name of the blob container to use as the root for the FTP file system.</summary>
    public string ContainerName { get; set; } = "ftp-root";
    /// <summary>The root path within the blob container to use as the base directory for the FTP file system.</summary>
    public string RootPath { get; set; } = "/";
}
