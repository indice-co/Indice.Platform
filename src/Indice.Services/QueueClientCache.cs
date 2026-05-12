using System.Collections.Concurrent;
using Azure.Storage.Queues;

namespace Indice.Services;

/// <summary>Thread-safe cache for Azure Storage Queue clients to avoid repeated instantiation and CreateIfNotExists calls.</summary>
public interface IQueueClientCache
{
    /// <summary>Gets or creates a QueueClient for the specified queue name and connection string.</summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="connectionStringName">The Azure Storage connection string name.</param>
    /// <param name="messageEncoding">The message encoding to use.</param>
    /// <returns>A cached or newly created QueueClient.</returns>
    Task<QueueClient> GetOrCreateAsync(string queueName, string connectionStringName, QueueMessageEncoding messageEncoding);
}

/// <inheritdoc/>
public sealed class QueueClientCache : IQueueClientCache
{
    private readonly record struct QueueClientCacheKey(string ConnectionStringName, string QueueName, QueueMessageEncoding MessageEncoding);

    private readonly ConcurrentDictionary<QueueClientCacheKey, Lazy<Task<QueueClient>>> _queueClients = new();

    private readonly AzureClientFactory _factory;
    /// <summary>Creates a new instance of <see cref="QueueClientCache"/>.</summary>
    public QueueClientCache(AzureClientFactory factory) {
        _factory = factory;
    }

    /// <inheritdoc/>
    public async Task<QueueClient> GetOrCreateAsync(string queueName, string connectionStringName, QueueMessageEncoding messageEncoding) {
        var cacheKey = new QueueClientCacheKey(connectionStringName, queueName, messageEncoding);

        var lazyClient = _queueClients.GetOrAdd(cacheKey, key => new Lazy<Task<QueueClient>>(async () => {
            var queueClient = _factory.CreateQueueClient(connectionStringName, queueName, new QueueClientOptions {
                MessageEncoding = messageEncoding
            });
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }));

        try {
            return await lazyClient.Value;
        }
        catch {
            // Remove the faulted task from cache so the next call can retry
            _queueClients.TryRemove(cacheKey, out _);
            throw;
        }
    }
}
