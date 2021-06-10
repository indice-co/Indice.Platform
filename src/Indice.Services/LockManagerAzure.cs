using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace Indice.Services
{
    /// <summary>
    /// <see cref="ILockManager"/> implementation with Azure blob storage as the backing store.
    /// </summary>
    public class LockManagerAzure : ILockManager
    {
        /// <summary>
        /// The cloud container client
        /// </summary>
        public BlobContainerClient BlobContainer { get; }

        /// <summary>
        /// Create the lockmanager
        /// </summary>
        /// <param name="options"></param>
        public LockManagerAzure(LockManagerAzureOptions options) {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.EnvironmentName == null) throw new ArgumentNullException(nameof(options.EnvironmentName));
            if (options.StorageConnection == null) throw new ArgumentNullException(nameof(options.StorageConnection));
            var environmentName = Regex.Replace(options.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            BlobContainer = new BlobContainerClient(options.StorageConnection, environmentName);
        }

        /// <inheritdoc />
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? duration = null) {
            await BlobContainer.CreateIfNotExistsAsync();
            var lockFileBlob = BlobContainer.GetBlobClient($"locks/{name}.lock");
            try {
                await lockFileBlob.UploadAsync(new MemoryStream(Encoding.ASCII.GetBytes("0")), overwrite: true);
                var lockFileLease = lockFileBlob.GetBlobLeaseClient();
                var leaseResponse = await lockFileLease.AcquireAsync(duration ?? TimeSpan.FromSeconds(30));
                return new LockLease(leaseResponse.Value.LeaseId, name, this);
            } catch (Exception ex) {
                throw new LockManagerException(name, ex);
            }
        }

        /// <inheritdoc />
        public async Task ReleaseLock(ILockLease @lock) {
            var lockFileBlob = BlobContainer.GetBlobClient($"locks/{@lock.Name}.lock");
            var lockFileLease = lockFileBlob.GetBlobLeaseClient(@lock.LeaseId);
            await lockFileLease.ReleaseAsync();
            // the following code that tries to delete had side-effects.
            // Not deleting is not problem whatsoever but 
            // there are multiple zero byte files that are never deleted on storage.
            //try {
            //    var response = await lockFileBlob.DeleteIfExistsAsync();
            //} catch {; }
        }

        /// <inheritdoc />
        public async Task<ILockLease> Renew(string name, string LeaseId) {
            var lockFileBlob = BlobContainer.GetBlobClient($"locks/{name}.lock");
            var lockFileLease = lockFileBlob.GetBlobLeaseClient(LeaseId);
            var leaseResponse = await lockFileLease.RenewAsync();
            return new LockLease(leaseResponse.Value.LeaseId, name, this);
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
