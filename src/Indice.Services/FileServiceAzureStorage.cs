using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Indice.Extensions;

namespace Indice.Services
{
    /// <summary>
    /// Azure Storage <see cref="IFileService"/> implementation.
    /// </summary>
    public class FileServiceAzureStorage : IFileService
    {
        /// <summary>
        /// The conection string parameter name. The setting key that will be searched inside the configuration.
        /// </summary>
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly string _environmentName;
        private readonly string _connectionString;

        /// <summary>
        /// Constructs the service.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Storage account.</param>
        /// <param name="environmentName">The environment name (ex. Development, Production).</param>
        public FileServiceAzureStorage(string connectionString, string environmentName) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (string.IsNullOrEmpty(environmentName)) {
                _environmentName = "production";
            }
            _connectionString = connectionString;
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
        }

        /// <summary>
        /// Save the file.
        /// </summary>
        /// <param name="filepath">The path that the file is saved to.</param>
        /// <param name="stream">The content of the file represented as a <see cref="Stream"/>.</param>
        public async Task SaveAsync(string filepath, Stream stream) {
            filepath = filepath.TrimStart('\\', '/');
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var container = new BlobContainerClient(_connectionString, folder);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient(filename);
            var extension = Path.GetExtension(filepath);
            stream.Position = 0;
            if (!string.IsNullOrEmpty(extension)) {
                var result = await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = FileExtensions.GetMimeType(extension) });
            } else {
                var result = await blob.UploadAsync(stream, overwrite: true);
            }
        }

        // Instead of streaming the blob through your server, you could download it directly from the blob storage.
        // http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
        /// <summary>
        /// Retrieve the data for a file.
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        public async Task<byte[]> GetAsync(string filepath) {
            filepath = filepath.TrimStart('\\', '/');
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
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

        /// <summary>
        /// Gets a path list for a given folder.
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        public async Task<IEnumerable<string>> SearchAsync(string filepath) {
            filepath = filepath.TrimStart('\\', '/');
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
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
        /// <summary>
        /// Get the file's properties (metadata).
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        public async Task<FileProperties> GetPropertiesAsync(string filepath) {
            filepath = filepath.TrimStart('\\', '/');
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var container = new BlobContainerClient(_connectionString, folder);
            var exists = await container.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"Container {folder} not found.");
            }
            var blob = container.GetBlobClient(filename);
            try {
                var response = await blob.GetPropertiesAsync();
                //TODO Remove when https://github.com/Azure/azure-sdk-for-net/issues/22877 is fixed
                var eTag = response.Value.ETag.ToString();
                if (!string.IsNullOrEmpty(eTag)) {
                    if (!(eTag.StartsWith("\"") || eTag.StartsWith("W"))) {
                        eTag = string.Format("\"{0}\"", eTag);
                    }
                }
                return new FileProperties {
                    CacheControl = response.Value.CacheControl,
                    ContentDisposition = response.Value.ContentDisposition,
                    ContentEncoding = response.Value.ContentEncoding,
                    ContentHash = System.Text.Encoding.UTF8.GetString(response.Value.ContentHash),
                    ContentType = response.Value.ContentType,
                    Length = response.Value.ContentLength,
                    ETag = eTag,
                    LastModified = response.Value.LastModified
                };
            } catch (RequestFailedException ex)
                  when (ex.ErrorCode == BlobErrorCode.BlobNotFound) {
                throw new FileNotFoundServiceException($"File {filename} not found.");
            }
        }

        /// <summary>
        /// Deletes a file or folder.
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        /// <param name="isDirectory">Determines if the <paramref name="filepath"/> points to a single file or a directory.</param>
        public async Task<bool> DeleteAsync(string filepath, bool isDirectory = false) {
            filepath = filepath.TrimStart('\\', '/');
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
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

        /// <summary>
        /// File service options specific to Azure.
        /// </summary>
        public class FileServiceOptions
        {
            /// <summary>
            /// The connection string to the Azure Storage account.
            /// </summary>
            public string ConnectionString { get; set; }
            /// <summary>
            /// The environment name (ex. Development, Production).
            /// </summary>
            public string EnvironmentName { get; set; }
        }
    }
}
