using System.Collections.Concurrent;
using Azure.Storage.Queues;

namespace Indice.Services;

/// <summary>Thread-safe cache for Azure Storage Queue clients to avoid repeated instantiation and CreateIfNotExists calls.</summary>
public interface IQueueClientCache
{
    /// <summary>Gets or creates a QueueClient for the specified queue name and connection string.</summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="connectionString">The Azure Storage connection string.</param>
    /// <param name="messageEncoding">The message encoding to use.</param>
    /// <returns>A cached or newly created QueueClient.</returns>
    Task<QueueClient> GetOrCreateAsync(string queueName, string connectionString, QueueMessageEncoding messageEncoding);
}

/// <inheritdoc/>
public sealed class QueueClientCache : IQueueClientCache
{
    private readonly ConcurrentDictionary<string, Lazy<Task<QueueClient>>> _queueClients = new();

    /// <inheritdoc/>
    public async Task<QueueClient> GetOrCreateAsync(string queueName, string connectionString, QueueMessageEncoding messageEncoding) {
        var cacheKey = $"{connectionString}::{queueName}::{messageEncoding}";

        var lazyClient = _queueClients.GetOrAdd(cacheKey, key => new Lazy<Task<QueueClient>>(async () => {
            var queueClient = new QueueClient(connectionString, queueName, new QueueClientOptions {
                MessageEncoding = messageEncoding
            });
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }));

        return await lazyClient.Value;
    }
}
