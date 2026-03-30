using Azure.Storage.Blobs.Models;
using FubarDev.FtpServer.FileSystem;

namespace Indice.Features.FtpServer.Azure;

/// <summary>
/// Represents a file entry in an Azure Blob Storage file system.
/// </summary>
/// <remarks>This class provides access to metadata and properties of a file stored in Azure Blob Storage. It
/// extends <see cref="AzureBlobFileSystemEntry"/> and implements <see cref="IUnixFileEntry"/>  to integrate with file
/// system abstractions.</remarks>
class AzureBlobFileEntry : AzureBlobFileSystemEntry, IUnixFileEntry
{
    public AzureBlobFileEntry(AzureBlobFileSystem fileSystem, BlobHierarchyItem item, long? fileSize)
        : base(fileSystem, item) {
        Size = fileSize ?? item.Blob.Properties.ContentLength ?? 0;
    }

    /// <summary>Gets the size of the item in bytes.</summary>
    public long Size { get; }
}
