using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Indice.Services
{
    public class FileServiceAzureStorage : IFileService
    {
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly CloudStorageAccount storageAccount;
        private readonly string environmentName;

        public FileServiceAzureStorage(string connectionString, string environmentName) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (string.IsNullOrEmpty(environmentName)) {
                environmentName = "production";
            }
            storageAccount = CloudStorageAccount.Parse(connectionString);
            environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
        }

        public async Task SaveAsync(string filepath, Stream stream) {
            var folder = environmentName ?? Path.GetDirectoryName(filepath);
            var filename = environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(filename);
            stream.Position = 0;
            await blob.UploadFromStreamAsync(stream);
        }


        // Instead of streaming the blob through your server, you could download it directly from the blob storage.
        // http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
        public async Task<byte[]> GetAsync(string filepath) {
            var folder = environmentName ?? Path.GetDirectoryName(filepath);
            var filename = environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = storageAccount.CreateCloudBlobClient();
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

        // Instead of streaming the blob through your server, you could download it directly from the blob storage.
        // http://stackoverflow.com/questions/24312527/azure-blob-storage-downloadtobytearray-vs-downloadtostream
        public async Task<FileProperties> GetPropertiesAsync(string filepath) {
            var folder = environmentName ?? Path.GetDirectoryName(filepath);
            var filename = environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = storageAccount.CreateCloudBlobClient();
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

        public async Task<bool> DeleteAsync(string filepath, bool isDirectory = false) {
            var folder = environmentName ?? Path.GetDirectoryName(filepath);
            var filename = environmentName == null ? filepath.Substring(folder.Length) : filepath;
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(folder);
            var exists = await container.ExistsAsync();

            if (!exists) {
                throw new FileNotFoundServiceException($"Container {folder} not found.");
            }

            var deleted = false;

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
    }
}
