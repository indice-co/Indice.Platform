using Azure.Storage.Blobs.Models;
using FubarDev.FtpServer.FileSystem;

namespace Indice.Features.FtpServer.Azure;

/// <summary>
/// Represents a directory entry in an Azure Blob Storage file system.
/// </summary>
/// <remarks>This class provides metadata and functionality specific to directories within an Azure Blob Storage
/// hierarchy. It supports distinguishing between root directories and other directories, as well as determining whether
/// a directory can be deleted.</remarks>
public class AzureBlobDirectoryEntry : AzureBlobFileSystemEntry, IUnixDirectoryEntry
{
    /// <summary>
    /// Represents a directory entry in an Azure Blob Storage file system.
    /// </summary>
    /// <param name="fileSystem">The file system to which this directory entry belongs.</param>
    /// <param name="directory">The underlying blob hierarchy item representing the directory.</param>
    /// <param name="isRoot">A value indicating whether this directory entry is the root directory.</param>
    public AzureBlobDirectoryEntry(AzureBlobFileSystem fileSystem, BlobHierarchyItem directory, bool isRoot)
        : base(fileSystem, directory) {
        IsRoot = isRoot;

    }

    /// <summary>Gets a value indicating whether the current item can be deleted.</summary>
    public bool IsDeletable => !IsRoot;
    
    /// <summary>Gets a value indicating whether the current node is the root of the hierarchy.</summary>
    public bool IsRoot { get; }

}
