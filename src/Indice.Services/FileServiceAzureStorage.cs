using System.Text.RegularExpressions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Indice.Extensions;

namespace Indice.Services;

/// <summary>Azure Storage <see cref="IFileService"/> implementation.</summary>
public class FileServiceAzureStorage : IFileService
{
    /// <summary>The connection string parameter name. The setting key that will be searched inside the configuration.</summary>
    public const string CONNECTION_STRING_NAME = "StorageConnection";
    private readonly string _containerName;
    private readonly string _connectionString;

    /// <summary>Constructs the service.</summary>
    /// <param name="connectionString">The connection string to the Azure Storage account.</param>
    /// <param name="containerName">Usually The environment name (ex. Development, Production).</param>
    public FileServiceAzureStorage(string connectionString, string containerName) {
        if (string.IsNullOrEmpty(connectionString)) {
            throw new ArgumentNullException(nameof(connectionString));
        }
        if (!string.IsNullOrEmpty(containerName)) {
            _containerName = Regex.Replace(containerName, @"\s+", "-").ToLowerInvariant();
        } else {
            _containerName = null;
        }
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string filePath, Stream stream, FileServiceSaveOptions saveOptions) {
        filePath = filePath.TrimStart('\\', '/');
        var folder = _containerName ?? Path.GetDirectoryName(filePath);
        var filename = _containerName == null ? filePath.Substring(folder.Length) : filePath;
        var container = new BlobContainerClient(_connectionString, folder);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(filename);
        var extension = Path.GetExtension(filePath);
        stream.Position = 0;
        if (!string.IsNullOrEmpty(extension)) {
            await blob.UploadAsync(stream, new BlobHttpHeaders { 
                ContentType = saveOptions?.ContentType ?? FileExtensions.GetMimeType(extension)
            });
        } else {
            await blob.UploadAsync(stream, overwrite: true);
        }
    }

    // Instead of streaming the blob through your server, you could download it directly from the blob storage.
    // http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
    /// <summary>Retrieve the data for a file.</summary>
    /// <param name="filePath">The path to the file.</param>
    public async Task<byte[]> GetAsync(string filePath) {
        filePath = filePath.TrimStart('\\', '/');
        var folder = _containerName ?? Path.GetDirectoryName(filePath);
        var filename = _containerName == null ? filePath.Substring(folder.Length) : filePath;
        var container = new BlobContainerClient(_connectionString, folder);
        await container.CreateIfNotExistsAsync();
        var exists = await container.ExistsAsync();
        if (!exists) {
            throw new FileNotFoundServiceException($"Container {folder} not found.");
        }
        var blob = container.GetBlobClient(filename);
        //exists = await blob.ExistsAsync();
        //if (!exists) {
        //    throw new FileNotFoundServiceException($"File {filename} not found.");
        //}
        try {
            using (var s = new MemoryStream()) {
                await blob.DownloadToAsync(s);
                return s.ToArray();
            }
        } catch (RequestFailedException ex)
              when (ex.ErrorCode == BlobErrorCode.BlobNotFound) {
            throw new FileNotFoundServiceException($"File {filename} not found.");
        }
    }

    /// <summary>Gets a path list for a given folder.</summary>
    /// <param name="filePath">The path to the file.</param>
    public async Task<IEnumerable<string>> SearchAsync(string filePath) {
        filePath = filePath.TrimStart('\\', '/');
        var folder = _containerName ?? Path.GetDirectoryName(filePath);
        var filename = _containerName == null ? filePath.Substring(folder.Length) : filePath;
        var container = new BlobContainerClient(_connectionString, folder);
        var exists = await container.ExistsAsync();
        if (!exists) {
            throw new FileNotFoundServiceException($"Container {folder} not found.");
        }
        var results = new List<string>();
        var segment = container.GetBlobsAsync(prefix: filename);

        // Enumerate the blobs returned for each page.
        await foreach (var blob in segment) {
            results.Add(blob.Name);
        }
        return results;
        /* in the future we may need to get the contents of a specific folder structure 
         * the following code will be verry usefull
        var results = new List<string>();
        var resultSegment = container.GetBlobsByHierarchyAsync(prefix: filename, delimiter: "/").AsPages(default, null);
        // Enumerate the blobs returned for each page.
        await foreach (var page in resultSegment) {
            results.AddRange(page.Values.Where(item => item.IsBlob).Select(item => $"{item.Prefix}{item.Blob.Name}"));
        }
        return results;
        */
    }

    // Instead of streaming the blob through your server, you could download it directly from the blob storage. http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
    /// <summary>Get the file's properties (metadata).</summary>
    /// <param name="filePath">The path to the file.</param>
    public async Task<FileProperties> GetPropertiesAsync(string filePath) {
        filePath = filePath.TrimStart('\\', '/');
        var folder = _containerName ?? Path.GetDirectoryName(filePath);
        var filename = _containerName == null ? filePath.Substring(folder.Length) : filePath;
        var container = new BlobContainerClient(_connectionString, folder);
        var exists = await container.ExistsAsync();
        if (!exists) {
            throw new FileNotFoundServiceException($"Container {folder} not found.");
        }
        var blob = container.GetBlobClient(filename);
        try {
            var response = await blob.GetPropertiesAsync();
            return new FileProperties {
                CacheControl = response.Value.CacheControl,
                ContentDisposition = response.Value.ContentDisposition,
                ContentEncoding = response.Value.ContentEncoding,
                ContentHash = System.Text.Encoding.UTF8.GetString(response.Value.ContentHash),
                ContentType = response.Value.ContentType,
                Length = response.Value.ContentLength,
                ETag = response.Value.ETag.GetHttpSafeETag(),
                LastModified = response.Value.LastModified
            };
        } catch (RequestFailedException exception) when (exception.ErrorCode == BlobErrorCode.BlobNotFound) {
            throw new FileNotFoundServiceException($"File {filename} not found.");
        }
    }

    /// <summary>Deletes a file or folder.</summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="isDirectory">Determines if the <paramref name="filePath"/> points to a single file or a directory.</param>
    public async Task<bool> DeleteAsync(string filePath, bool isDirectory = false) {
        filePath = filePath.TrimStart('\\', '/');
        var folder = _containerName ?? Path.GetDirectoryName(filePath);
        var filename = _containerName == null ? filePath.Substring(folder.Length) : filePath;
        var container = new BlobContainerClient(_connectionString, folder);
        var exists = await container.ExistsAsync();
        if (!exists) {
            throw new FileNotFoundServiceException($"Container {folder} not found.");
        }
        bool deleted;
        if (!isDirectory) {
            var blob = container.GetBlobClient(filename);
            deleted = await blob.DeleteIfExistsAsync();
        } else {
            var segment = container.GetBlobsAsync(prefix: filename);
            await foreach (var blob in segment) {
                await container.DeleteBlobIfExistsAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots);
            }
            deleted = true;
        }
        return deleted;
    }
}

/// <summary>File service options specific to Azure.</summary>
public class FileServiceAzureOptions
{
    /// <summary>The connection string setting name pointing to the to the Azure Storage account.</summary>
    public string ConnectionStringName { get; set; }
    /// <summary>Usually the environment name (ex. Development, Production).</summary>
    public string ContainerName { get; set; }
}
