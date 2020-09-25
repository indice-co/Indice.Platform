using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace Indice.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class LockManagerAzure : ILockManager
    {
        /// <summary>
        /// 
        /// </summary>
        public CloudBlobContainer BlobContainer { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="hostingEnvironment"></param>
        public LockManagerAzure(IConfiguration configuration, IHostingEnvironment hostingEnvironment) {
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var environment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            var environmentName = Regex.Replace(environment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            var storageAccount = CloudStorageAccount.Parse(configuration.GetConnectionString("StorageConnection"));
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
    }
}
