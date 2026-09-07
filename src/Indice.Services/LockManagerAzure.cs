using System.Text;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace Indice.Services;

/// <summary><see cref="ILockManager"/> implementation with Azure Blob Storage as the backing store.</summary>
public class LockManagerAzure : ILockManager
{
    /// <summary>
    /// Azure minimum duration a lock lease can be acquired for
    /// </summary>
    private const int MIN_LOCK_DURATION_SECONDS = 15;

    /// <summary>
    /// Azure maximum duration a lock lease can be acquired for
    /// </summary>
    private const int MAX_LOCK_DURATION_SECONDS = 60;

    /// <summary>The default name of the storage connection string.</summary>
    public const string CONNECTION_STRING_NAME = "StorageConnection";

    /// <summary>Creates a new instance of <see cref="LockManagerAzure"/>.</summary>
    /// <param name="factory">The Azure client factory.</param>
    /// <param name="options">The options for configuring the lock manager.</param>
    public LockManagerAzure(AzureClientFactory factory, LockManagerAzureOptions options) {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.EnvironmentName)) {
            throw new ArgumentNullException(nameof(options.EnvironmentName));
        }
        if (string.IsNullOrWhiteSpace(options.ConnectionStringName)) {
            throw new ArgumentNullException(nameof(options.ConnectionStringName));
        }
        var environmentName = Regex.Replace(options.EnvironmentName, @"\s+", "-").ToLowerInvariant();
        BlobContainer = factory.GetOrCreateBlobContainerClient(options.ConnectionStringName, environmentName);
    }

    /// <summary>The cloud container client.</summary>
    public BlobContainerClient BlobContainer { get; }

    /// <summary>Acquire a lock or throws.</summary>
    /// <param name="name">Topic or name.</param>
    /// <param name="duration">The duration the lease will be active. The duration can be -1 (infinite) or between 15 and 60 seconds. Defaults to 30 seconds.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <exception cref="LockManagerException">Occurs when the lock cannot be acquired.</exception>
    public async Task<ILockLease> AcquireLock(string name, TimeSpan? duration = null, CancellationToken cancellationToken = default) {
        if (duration is not null) {
            if (duration.Value.TotalSeconds < MIN_LOCK_DURATION_SECONDS && duration.Value != TimeSpan.FromSeconds(-1)) {
                var innerException = new ArgumentOutOfRangeException(nameof(duration), duration.Value.TotalSeconds, $"Duration is less than minimum duration of {MIN_LOCK_DURATION_SECONDS} seconds");
                throw new LockManagerException(name, innerException);
            }

            if (duration.Value.TotalSeconds > MAX_LOCK_DURATION_SECONDS) {
                var innerException = new ArgumentOutOfRangeException(nameof(duration), duration.Value.TotalSeconds, $"Duration exceeds the maximum duration of {MAX_LOCK_DURATION_SECONDS} seconds");
                throw new LockManagerException(name, innerException);
            }
        }

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

/// <summary>Options that allow lockmanager configuration</summary>
public class LockManagerAzureOptions
{
    /// <summary>Hosting environment name.</summary>
    public string? EnvironmentName { get; set; }
    /// <summary>Storage connection.</summary>
    public string? ConnectionStringName { get; set; }

}
