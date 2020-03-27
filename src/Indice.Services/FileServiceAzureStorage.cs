using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Indice.Extensions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

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
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _environmentName;

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
            _storageAccount = CloudStorageAccount.Parse(connectionString);
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
        }

        /// <summary>
        /// Save the file.
        /// </summary>
        /// <param name="filepath">The path that the file is saved to.</param>
        /// <param name="stream">The content of the file represented as a <see cref="Stream"/>.</param>
        public async Task SaveAsync(string filepath, Stream stream) {
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = _storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(filename);
            var extension = Path.GetExtension(filepath);
            if (!string.IsNullOrEmpty(extension)) {
                blob.Properties.ContentType = FileExtensions.GetMimeType(extension);
            }
            stream.Position = 0;
            await blob.UploadFromStreamAsync(stream);
        }

        // Instead of streaming the blob through your server, you could download it directly from the blob storage.
        // http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
        /// <summary>
        /// Retrieve the data for a file.
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        public async Task<byte[]> GetAsync(string filepath) {
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = _storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            var exists = await container.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"Container {folder} not found.");
            }
            var blob = container.GetBlockBlobReference(filename);
            exists = await blob.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"File {filename} not found.");
            }
            await blob.FetchAttributesAsync();
            var bytesLength = blob.Properties.Length;
            var bytes = new byte[bytesLength];
            for (var i = 0; i < bytesLength; i++) {
                bytes[i] = 0x20;
            }
            await blob.DownloadToByteArrayAsync(bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Gets a path list for a given folder.
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        public async Task<IEnumerable<string>> SearchAsync(string filepath) {
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = _storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            var exists = await container.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"Container {folder} not found.");
            }
            var results = new List<string>();
            var directory = container.GetDirectoryReference(filename);
            var list = await directory.ListBlobsSegmentedAsync(true, BlobListingDetails.All, null, null, null, null);
            foreach (var blob in list.Results) {
                if (blob.GetType() == typeof(CloudBlob) || blob.GetType().BaseType == typeof(CloudBlob)) {
                    results.Add(blob.Uri.ToString());
                }
            }
            return results;
        }

        // Instead of streaming the blob through your server, you could download it directly from the blob storage. http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
        /// <summary>
        /// Get the file's properties (metadata).
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        public async Task<FileProperties> GetPropertiesAsync(string filepath) {
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = _storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            var exists = await container.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"Container {folder} not found.");
            }
            var blob = container.GetBlockBlobReference(filename);
            exists = await blob.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"File {filename} not found.");
            }
            await blob.FetchAttributesAsync();
            return new FileProperties {
                CacheControl = blob.Properties.CacheControl,
                ContentDisposition = blob.Properties.ContentDisposition,
                ContentEncoding = blob.Properties.ContentEncoding,
                ContentMD5 = blob.Properties.ContentMD5,
                ContentType = blob.Properties.ContentType,
                Length = blob.Properties.Length,
                ETag = blob.Properties.ETag,
                LastModified = blob.Properties.LastModified
            };
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filepath">The path to the file.</param>
        /// <param name="isDirectory">Determines if the <paramref name="filepath"/> points to a single file or a directory.</param>
        public async Task<bool> DeleteAsync(string filepath, bool isDirectory = false) {
            var folder = _environmentName ?? Path.GetDirectoryName(filepath);
            var filename = _environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = _storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            var exists = await container.ExistsAsync();
            if (!exists) {
                throw new FileNotFoundServiceException($"Container {folder} not found.");
            }
            bool deleted;
            if (!isDirectory) {
                var blob = container.GetBlockBlobReference(filename);
                deleted = await blob.DeleteIfExistsAsync();
            } else {
                var directory = container.GetDirectoryReference(filename);
                var list = await directory.ListBlobsSegmentedAsync(true, BlobListingDetails.All, null, null, null, null);
                foreach (var blob in list.Results) {
                    if (blob.GetType() == typeof(CloudBlob) || blob.GetType().BaseType == typeof(CloudBlob)) {
                        await ((CloudBlob)blob).DeleteIfExistsAsync();
                    }
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
