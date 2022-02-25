using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace Indice.Services
{
    /// <summary>
    /// <see cref="ILockManager"/> implementation with Azure Blob Storage as the backing store.
    /// </summary>
    public class LockManagerAzure : ILockManager
    {
        /// <summary>
        /// The default name of the storage connection string.
        /// </summary>
        public const string CONNECTION_STRING_NAME = "StorageConnection";

        /// <summary>
        /// Creates a new instance of <see cref="LockManagerAzure"/>.
        /// </summary>
        /// <param name="options"></param>
        public LockManagerAzure(LockManagerAzureOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (string.IsNullOrWhiteSpace(options.EnvironmentName)) {
                throw new ArgumentNullException(nameof(options.EnvironmentName));
            }
            if (string.IsNullOrWhiteSpace(options.ConnectionString)) {
                throw new ArgumentNullException(nameof(options.ConnectionString));
            }
            var environmentName = Regex.Replace(options.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            BlobContainer = new BlobContainerClient(options.ConnectionString, environmentName);
        }

        /// <summary>
        /// The cloud container client.
        /// </summary>
        public BlobContainerClient BlobContainer { get; }

        /// <inheritdoc />
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? duration = null, CancellationToken cancellationToken = default) {
            await BlobContainer.CreateIfNotExistsAsync();
            var lockFileBlob = BlobContainer.GetBlobClient($"locks/{name}.lock");
            try {
                await lockFileBlob.UploadAsync(new MemoryStream(Encoding.ASCII.GetBytes("0")), overwrite: true);
                var lockFileLease = lockFileBlob.GetBlobLeaseClient();
                var leaseResponse = await lockFileLease.AcquireAsync(duration ?? TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
                return new LockLease(leaseResponse.Value.LeaseId, name, this);
            } catch (Exception exception) {
                throw new LockManagerException(name, exception);
            }
        }

        /// <inheritdoc />
        public async Task ReleaseLock(ILockLease @lock) {
            var lockFileBlob = BlobContainer.GetBlobClient($"locks/{@lock.Name}.lock");
            var lockFileLease = lockFileBlob.GetBlobLeaseClient(@lock.LeaseId);
            await lockFileLease.ReleaseAsync();
        }

        /// <inheritdoc />
        public async Task<ILockLease> Renew(string name, string leaseId, CancellationToken cancellationToken = default) {
            var lockFileBlob = BlobContainer.GetBlobClient($"locks/{name}.lock");
            var lockFileLease = lockFileBlob.GetBlobLeaseClient(leaseId);
            var leaseResponse = await lockFileLease.RenewAsync(cancellationToken: cancellationToken);
            return new LockLease(leaseResponse.Value.LeaseId, name, this);
        }

        /// <inheritdoc />
        public Task Cleanup() => Task.CompletedTask;
    }

    /// <summary>
    /// Options that allow lockmanager configuration
    /// </summary>
    public class LockManagerAzureOptions
    {
        /// <summary>
        /// Hosting environment name.
        /// </summary>
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Storage connection.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
