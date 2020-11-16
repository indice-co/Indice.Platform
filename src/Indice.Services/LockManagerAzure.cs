using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Indice.Services
{
    /// <summary>
    /// <see cref="ILockManager"/> implementation with Azure blob storage as the backing store.
    /// </summary>
    public class LockManagerAzure : ILockManager
    {
        /// <summary>
        /// 
        /// </summary>
        public CloudBlobContainer BlobContainer { get; }

        /// <summary>
        /// Create the lockmanager
        /// </summary>
        /// <param name="options"></param>
        public LockManagerAzure(LockManagerAzureOptions options) {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.EnvironmentName == null) throw new ArgumentNullException(nameof(options.EnvironmentName));
            if (options.StorageConnection == null) throw new ArgumentNullException(nameof(options.StorageConnection));
            var environmentName = Regex.Replace(options.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            var storageAccount = CloudStorageAccount.Parse(options.StorageConnection);
            var blobClient = storageAccount.CreateCloudBlobClient();
            BlobContainer = blobClient.GetContainerReference(environmentName);
        }

        /// <inheritdoc />
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null) {
            await BlobContainer.CreateIfNotExistsAsync();
            var lockFileBlob = BlobContainer.GetBlockBlobReference($"{name}.lock");
            await lockFileBlob.UploadTextAsync(string.Empty);
            var leaseId = await lockFileBlob.AcquireLeaseAsync(timeout, null, AccessCondition.GenerateIfNotExistsCondition(), null, null);
            return new LockLease(leaseId, name, this);
        }

        /// <inheritdoc />
        public Task ReleaseLock(ILockLease @lock) {
            var lockFileBlob = BlobContainer.GetBlockBlobReference($"{@lock.Name}.lock");
            return lockFileBlob.ReleaseLeaseAsync(new AccessCondition {
                LeaseId = @lock.LeaseId
            });
        }

        /// <inheritdoc />
        public Task Cleanup() {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Options that allow lockmanager configuration
    /// </summary>
    public class LockManagerAzureOptions
    {
        /// <summary>
        /// Hosting environment name
        /// </summary>
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Storage connection.
        /// </summary>
        public string StorageConnection { get; set; }
    }
}