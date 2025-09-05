using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FubarDev.FtpServer.BackgroundTransfer;
using FubarDev.FtpServer.FileSystem;

namespace Indice.Features.FtpServer.Azure;

/// <summary>
/// Provides an implementation of a Unix-like file system backed by Azure Blob Storage.
/// </summary>
/// <remarks>This class enables interaction with Azure Blob Storage as if it were a Unix-like file system.  It
/// supports operations such as creating directories, reading and writing files, moving entries,  and deleting entries.
/// The file system is case-insensitive and uses a forward slash ("/") as the  directory delimiter. </remarks>
public class AzureBlobFileSystem(BlobContainerClient container, string rootPath) : IUnixFileSystem
{
    /// <summary>
    /// Represents the name of the placeholder file used to mark a directory exists in blob storage.
    /// </summary>
    /// <remarks>This constant is typically used to create or identify a file that serves as a marker within a
    /// directory, ensuring the directory existance even if there are no files within.</remarks>
    public const string DIRECTORY_HOLDER_FILE_NAME = "___dirholder___.txt";
    private const string DIRECTORY_HOLDER_TEXT = "This is just a placeholder for the directory.";
    private const string Delimiter = "/";

    /// <summary>Gets the root path of the file system within the Azure Blob Storage container.</summary>
    protected string RootPath { get; } = rootPath.Replace("\\", Delimiter);
    /// <summary>Gets the Azure Blob Storage container client used by this file system.</summary>
    protected BlobContainerClient Container { get; } = container;

    /// <inheritdoc/>
    public bool SupportsNonEmptyDirectoryDelete => true;
    /// <inheritdoc/>
    public IUnixDirectoryEntry Root { get; private set; } = null!;
    /// <inheritdoc/>
    public bool SupportsAppend => false;
    /// <inheritdoc/>
    public StringComparer FileSystemEntryComparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Initializes the root directory by ensuring its existence and loading its metadata.
    /// </summary>
    /// <remarks>This method ensures that the root directory exists in the Azure Blob Storage container.  If
    /// the directory contains any blobs or subdirectories, the metadata for the first entry  is used to initialize the
    /// <see cref="Root"/> property.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    internal async Task InitAsync() {
        // ensure root directory exists
        await CreateAzureBlobDirectoryAsync(RootPath);
        var resultSegment = Container.GetBlobsByHierarchyAsync(prefix: RootPath, delimiter: Delimiter).AsPages(default, null);
        await foreach (var page in resultSegment) {
            if (page.Values.Count > 0) {

                Root = new AzureBlobDirectoryEntry(this, page.Values.First(), true);
                break;
            }
        }

    }

    private async Task CreateAzureBlobDirectoryAsync(string name) {
        var dirblock = Container.GetBlobClient(name + Delimiter + DIRECTORY_HOLDER_FILE_NAME);
        if (await dirblock.ExistsAsync())
            return;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(DIRECTORY_HOLDER_TEXT));
        await dirblock.UploadAsync(stream);
    }

    /// <summary>
    /// Gets a list of <see cref="IUnixFileSystemEntry"/> objects for a given <paramref name="directoryEntry"/>.
    /// </summary>
    /// <param name="directoryEntry">The directory to search contents for</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of silesystem entries</returns>
    public async Task<IReadOnlyList<IUnixFileSystemEntry>> GetEntriesAsync(IUnixDirectoryEntry directoryEntry, CancellationToken cancellationToken) {
        var dir = ((AzureBlobDirectoryEntry)directoryEntry).Item;

        var result = new List<IUnixFileSystemEntry>();

        var resultSegment = Container.GetBlobsByHierarchyAsync(prefix: dir.Prefix, delimiter: Delimiter).AsPages(default, null);

        await foreach (var page in resultSegment) {
            foreach (var item in page.Values) {
                if (item.IsBlob) {
                    // hide the directory holder file from the client
                    if (Path.GetFileName(item.Blob.Name).Equals(DIRECTORY_HOLDER_FILE_NAME))
                        continue;
                    result.Add(new AzureBlobFileEntry(this, item, item.Blob.Properties.ContentLength));
                } else if (item.IsPrefix) {
                    result.Add(new AzureBlobDirectoryEntry(this, item, false));
                }
            }

        }
        return result;
    }


    /// <inheritdoc/>
    public async Task<IUnixFileSystemEntry?> GetEntryByNameAsync(IUnixDirectoryEntry directoryEntry, string name, CancellationToken cancellationToken) {
        var dir = ((AzureBlobDirectoryEntry)directoryEntry).Item;
        var resultSegment = Container.GetBlobsByHierarchyAsync(prefix: dir.Prefix, delimiter: Delimiter).AsPages(default, null);

        await foreach (var page in resultSegment) {
            foreach (var item in page.Values) {
                if (item.IsBlob) {
                    var abf = new AzureBlobFileEntry(this, item, item.Blob.Properties.ContentLength);
                    if (abf.Name == name)
                        return abf;
                } else if (item.IsPrefix) {
                    var abd = new AzureBlobDirectoryEntry(this, item, false);
                    if (abd.Name == name)
                        return abd;
                }
            }
        }
        return null;

    }

    /// <inheritdoc/>
    public async Task<IUnixFileSystemEntry> MoveAsync(IUnixDirectoryEntry parent, IUnixFileSystemEntry source, IUnixDirectoryEntry target, string fileName, CancellationToken cancellationToken) {
        // just file first
        if (((AzureBlobFileSystemEntry)source).IsFolder)
            throw new NotImplementedException();

        var file = ((AzureBlobFileEntry)source).Item;

        var dir = ((AzureBlobDirectoryEntry)target).Item;
        var sourceBlob = Container.GetBlobClient(dir.Prefix + file.Blob.Name);
        var destblob = Container.GetBlobClient(dir.Prefix + fileName);

        var copyOperation = await destblob.StartCopyFromUriAsync(sourceBlob.Uri);
        while (!copyOperation.HasCompleted) {
            await copyOperation.WaitForCompletionAsync();
            await Task.Delay(100);
        }
        await sourceBlob.DeleteAsync();
        return await GetEntryByNameAsync(target, fileName, cancellationToken) ?? throw new ApplicationException("Failed to move file");
    }

    /// <inheritdoc/>
    public async Task UnlinkAsync(IUnixFileSystemEntry entry, CancellationToken cancellationToken) {
        if (((AzureBlobFileSystemEntry)entry).IsFolder) {
            var dir = ((AzureBlobDirectoryEntry)entry).Item;
            var segment = Container.GetBlobsAsync(prefix: dir.Prefix);
            await foreach (var blob in segment) {
                await Container.DeleteBlobIfExistsAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots);
            }
        } else {
            var file = ((AzureBlobFileEntry)entry).Item;
            var blob = Container.GetBlobClient(file.Prefix + file.Blob.Name);
            await blob.DeleteIfExistsAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<IUnixDirectoryEntry> CreateDirectoryAsync(IUnixDirectoryEntry targetDirectory, string directoryName, CancellationToken cancellationToken) {
        var dir = ((AzureBlobDirectoryEntry)targetDirectory).Item;
        await CreateAzureBlobDirectoryAsync(dir.Prefix + directoryName);

        return (IUnixDirectoryEntry)(await GetEntryByNameAsync(targetDirectory, directoryName, cancellationToken))!;
    }

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(IUnixFileEntry fileEntry, long startPosition, CancellationToken cancellationToken) {
        var file = ((AzureBlobFileEntry)fileEntry).Item;
        var blobclient = Container.GetBlobClient(file.Prefix + file.Blob.Name);
        return blobclient.OpenReadAsync();
    }

    /// <inheritdoc/>
    public async Task<IBackgroundTransfer?> CreateAsync(IUnixDirectoryEntry targetDirectory, string fileName, Stream data, CancellationToken cancellationToken) {
        var dir = ((AzureBlobDirectoryEntry)targetDirectory).Item;
        var blockblob = Container.GetBlobClient(dir.Prefix + fileName);
        await blockblob.UploadAsync(data);

        return null;
    }

    /// <inheritdoc/>
    public async Task<IBackgroundTransfer?> ReplaceAsync(IUnixFileEntry fileEntry, Stream data, CancellationToken cancellationToken) {
        var file = ((AzureBlobFileEntry)fileEntry).Item;
        var blockblob = Container.GetBlobClient(file.Prefix + file.Blob.Name);
        await blockblob.UploadAsync(data);

        return null;
    }

    /// <inheritdoc/>
    public Task<IUnixFileSystemEntry> SetMacTimeAsync(IUnixFileSystemEntry entry, DateTimeOffset? modify, DateTimeOffset? access, DateTimeOffset? create, CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IBackgroundTransfer?> AppendAsync(IUnixFileEntry fileEntry, long? startPosition, Stream data, CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }

}
