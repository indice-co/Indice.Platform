using System.Collections.Concurrent;
using Azure.Storage.Blobs;

namespace Indice.Services;

/// <summary>Thread-safe cache for Azure Storage Blob clients to avoid repeated instantiation and CreateIfNotExists calls.</summary>
public interface IBlobContainerClientCache
{
    /// <summary>Gets or creates a BlobContainerClient for the specified container name and connection string.</summary>
    /// <param name="connectionStringName">The Azure Storage connection string name.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <returns>A cached or newly created BlobClient.</returns>
    Task<BlobContainerClient> GetOrCreateAsync(string connectionStringName, string containerName);
}

/// <inheritdoc/>
public sealed class BlobContainerClientCache : IBlobContainerClientCache
{
    private readonly record struct BlobClientCacheKey(string ConnectionStringName, string ContainerName);

    private readonly ConcurrentDictionary<BlobClientCacheKey, Lazy<Task<BlobContainerClient>>> _blobClients = new();
    private readonly AzureClientFactory _factory;
    /// <summary>
    /// Initializes a new instance of the BlobClientCache class using the specified Azure client factory.
    /// </summary>
    /// <param name="factory">The AzureClientFactory instance used to create and manage BlobContlient instances. Cannot be null.</param>
    public BlobContainerClientCache(AzureClientFactory factory) {
        _factory = factory;
    }
            
    /// <inheritdoc/>
    public async Task<BlobContainerClient> GetOrCreateAsync(string connectionStringName, string containerName) {
        var cacheKey = new BlobClientCacheKey(connectionStringName, containerName);

        var lazyClient = _blobClients.GetOrAdd(cacheKey, key => new Lazy<Task<BlobContainerClient>>(async () => {
            var blobClient = _factory.CreateBlobContainerClient(connectionStringName, containerName);
            await blobClient.CreateIfNotExistsAsync();
            return blobClient;
        }));

        try {
            return await lazyClient.Value;
        }
        catch {
            // Remove the faulted task from cache so the next call can retry
            _blobClients.TryRemove(cacheKey, out _);
            throw;
        }
    }
}
