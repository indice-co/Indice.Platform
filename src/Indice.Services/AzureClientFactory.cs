using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace Indice.Services;

/// <summary>
/// Client factory for Azure services such as Blob Storage, Queue Storage, and Service Bus. 
/// It supports both connection string-based and Azure AD credential-based authentication. 
/// The factory retrieves configuration settings from the provided IConfiguration instance 
/// to create the appropriate clients based on the specified connection string name and service type.
/// </summary>
public class AzureClientFactory
{
    private readonly record struct BlobContainerCacheKey(string ConnectionStringName, string ContainerName);
    private readonly record struct QueueCacheKey(string ConnectionStringName, string QueueName, QueueMessageEncoding MessageEncoding);

    private readonly ConcurrentDictionary<BlobContainerCacheKey, Lazy<Task<BlobContainerClient>>> _blobContainerClients = new();
    private readonly ConcurrentDictionary<QueueCacheKey, Lazy<Task<QueueClient>>> _queueClients = new();
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureClientFactory"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance to retrieve Azure service settings from.</param>
    public AzureClientFactory(IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        _configuration = configuration;
    }


    /// <summary>
    /// Gets or creates a cached <see cref="BlobContainerClient"/> for the specified connection string name and container name.
    /// Creates the container if it does not exist.
    /// </summary>
    /// <param name="connectionStringName">Configuration key used to resolve the Azure Storage connection.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <returns>A cached <see cref="BlobContainerClient"/> instance.</returns>
    public async Task<BlobContainerClient> GetOrCreateBlobContainerClientAsync(string connectionStringName, string containerName) {
        var cacheKey = new BlobContainerCacheKey(connectionStringName, containerName);
        var lazyClient = _blobContainerClients.GetOrAdd(cacheKey, _ => new Lazy<Task<BlobContainerClient>>(async () => {
            var client = CreateBlobContainerClient(connectionStringName, containerName);
            await client.CreateIfNotExistsAsync();
            return client;
        }));
        try {
            return await lazyClient.Value;
        } catch {
            _blobContainerClients.TryRemove(cacheKey, out _);
            throw;
        }
    }
    private BlobContainerClient CreateBlobContainerClient(string connectionStringName, string containerName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        var storageConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new BlobContainerClient(storageConnection, containerName);
        }
        storageConnection = _configuration.GetValue<string>(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new BlobContainerClient(storageConnection, containerName);
        }
        var credential = CreateAzureCredential(connectionStringName);
        var accountName = _configuration.GetSection(connectionStringName).GetValue<string>("accountName");
        if (string.IsNullOrWhiteSpace(accountName)) {
            throw new ArgumentNullException($"\"{connectionStringName}__accountName\" is missing.");
        }
        var blobUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        return new BlobContainerClient(blobUri, credential);

    }
    /// <summary>
    /// Creates a <see cref="BlobContainerClient"/> instance using the provided connection string value directly and container name.
    /// </summary>
    /// <param name="connectionString">The actual Azure Storage connection string.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <returns>A <see cref="BlobContainerClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> or <paramref name="containerName"/> is null or empty.</exception>
    public async Task<BlobContainerClient> GetOrCreateBlobContainerClientAsyncWithConnectionString(string connectionString, string containerName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName); var cacheKey = new BlobContainerCacheKey(connectionString, containerName);
        var lazyClient = _blobContainerClients.GetOrAdd(cacheKey, _ => new Lazy<Task<BlobContainerClient>>(async () => {
            var client = new BlobContainerClient(connectionString, containerName);
            await client.CreateIfNotExistsAsync();
            return client;
        }));
        try {
            return await lazyClient.Value;
        } catch {
            _blobContainerClients.TryRemove(cacheKey, out _);
            throw;
        }
    }

    /// <summary>
    /// Creates a <see cref="BlobServiceClient"/> using either:
    /// <para>- A full connection string</para>
    /// <para>- Azure identity-based configuration using account name and credential</para>
    /// </summary>
    /// <param name="connectionStringName">Configuration key used to resolve either connection string or identity-based settings.</param>
    /// <returns>A fully configured <see cref="BlobServiceClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionStringName"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when identity-based configuration is used but account name is missing.</exception>
    public BlobServiceClient CreateBlobServiceClient(string connectionStringName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);
        var storageConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new BlobServiceClient(storageConnection);
        }

        storageConnection = _configuration.GetValue<string>(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new BlobServiceClient(storageConnection);
        }

        var credential = CreateAzureCredential(connectionStringName);
        var accountName = _configuration.GetSection(connectionStringName).GetValue<string>("accountName");
        if (string.IsNullOrWhiteSpace(accountName)) {
            throw new ArgumentNullException($"\"{connectionStringName}__accountName\" is missing.");
        }
        var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
        return new BlobServiceClient(blobUri, credential);
    }

    /// <summary>
    /// Gets or creates a cached <see cref="BlobServiceClient"/> using the provided connection string value directly.
    /// </summary>
    /// <param name="connectionString">The actual Azure Storage connection string.</param>
    /// <returns>A cached <see cref="BlobServiceClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public BlobServiceClient GetOrCreateCreateBlobServiceClientWithConnectionString(string connectionString) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new BlobServiceClient(connectionString);
    }



    /// <summary>
    /// Gets or creates a cached <see cref="QueueClient"/> using the provided connection string value directly.
    /// </summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="connectionStringName">The configuration key used to resolve either connection string or identity-based settings.</param>
    /// <param name="messageEncoding">The message encoding to use.</param>
    /// <returns>A cached <see cref="QueueClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="queueName"/> or <paramref name="connectionStringName"/> is null or empty.</exception>
    public async Task<QueueClient> GetOrCreateQueueClientAsync(string queueName, string connectionStringName, QueueMessageEncoding messageEncoding) {
        var cacheKey = new QueueCacheKey(connectionStringName, queueName, messageEncoding);

        var lazyClient = _queueClients.GetOrAdd(cacheKey, key => new Lazy<Task<QueueClient>>(async () => {
            var queueClient = CreateQueueClient(connectionStringName, queueName, new QueueClientOptions {
                MessageEncoding = messageEncoding
            });
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }));

        try {
            return await lazyClient.Value;
        } catch {
            // Remove the faulted task from cache so the next call can retry
            _queueClients.TryRemove(cacheKey, out _);
            throw;
        }
    }
    private QueueClient CreateQueueClient(string connectionStringName, string queueName, QueueClientOptions? options = null) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Storage ConnectionStringName is required.");
        }
        if (string.IsNullOrWhiteSpace(queueName)) {
            throw new ArgumentNullException("Storage queue name is required.");
        }
        var storageConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new QueueClient(storageConnection, queueName, options);
        }
        storageConnection = _configuration.GetValue<string>(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new QueueClient(storageConnection, queueName, options);
        }
        var credential = CreateAzureCredential(connectionStringName);
        var accountName = _configuration.GetSection(connectionStringName).GetValue<string>("accountName");
        if (string.IsNullOrWhiteSpace(accountName)) {
            throw new ArgumentNullException($"\"{connectionStringName}__accountName\" is missing.");
        }
        var queueUri = new Uri($"https://{accountName}.queue.core.windows.net/{queueName}");
        return new QueueClient(queueUri, credential, options);
    }

    /// <summary>
    /// Creates a <see cref="QueueClient"/> instance using the provided connection string value directly and queue name.
    /// </summary>
    /// <param name="connectionString">The actual Azure Storage connection string.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="messageEncoding">The message encoding to use.</param>
    /// <returns>A <see cref="QueueClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> or <paramref name="queueName"/> is null or empty.</exception>
    public async Task<QueueClient> CreateQueueClientWithConnectionString(string connectionString, string queueName, QueueMessageEncoding messageEncoding) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var cacheKey = new QueueCacheKey(connectionString, queueName, messageEncoding);

        var lazyClient = _queueClients.GetOrAdd(cacheKey, key => new Lazy<Task<QueueClient>>(async () => {
            var queueClient = new QueueClient(connectionString, queueName, new QueueClientOptions {
                MessageEncoding = messageEncoding
            });
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }));

        try {
            return await lazyClient.Value;
        } catch {
            // Remove the faulted task from cache so the next call can retry
            _queueClients.TryRemove(cacheKey, out _);
            throw;
        }
    }

    /// <summary>
    /// Creates and returns a new instance of the ServiceBusClient using the specified connection string name or
    /// associated Azure credentials.
    /// </summary>
    /// <remarks>If a connection string is found for the specified name, it is used to create the
    /// ServiceBusClient. Otherwise, the method attempts to create the client using Azure credentials and a fully
    /// qualified namespace from configuration.</remarks>
    /// <param name="connectionStringName">The name of the connection string or configuration key used to retrieve Service Bus connection information.</param>
    /// <returns>A ServiceBusClient instance configured to connect to the Azure Service Bus namespace specified by the provided connection string name.</returns>
    /// <exception cref="ArgumentNullException">Thrown if connectionStringName is null, empty, or consists only of white-space characters.</exception>
    public ServiceBusClient CreateServiceBusClient(string connectionStringName) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Service Bus ConnectionStringName is required.");
        }

        var serviceBusConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(serviceBusConnection)) {
            return new ServiceBusClient(serviceBusConnection);
        }
        serviceBusConnection = _configuration.GetValue<string>(connectionStringName);
        if (!string.IsNullOrWhiteSpace(serviceBusConnection)) {
            return new ServiceBusClient(serviceBusConnection);
        }

        var credential = CreateAzureCredential(connectionStringName);
        var namespaceName = _configuration.GetSection(connectionStringName).GetValue<string>("fullyQualifiedNamespace");
        if (string.IsNullOrWhiteSpace(namespaceName)) {
            throw new ArgumentNullException($"\"{connectionStringName}__fullyQualifiedNamespace\" is missing.");
        }
        return new ServiceBusClient(namespaceName, credential);
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusClient"/> using the provided connection string value directly.
    /// </summary>
    /// <param name="connectionString">The actual Azure Service Bus connection string.</param>
    /// <returns>A <see cref="ServiceBusClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public ServiceBusClient CreateServiceBusClientWithConnectionString(string connectionString) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new ServiceBusClient(connectionString);
    }

    /// <summary>
    /// Creates a new instance of the ServiceBusAdministrationClient using the specified connection string name or
    /// associated Azure credentials.
    /// </summary>
    /// <remarks>If a connection string is found for the specified name, it is used to create the client.
    /// Otherwise, the method attempts to create the client using Azure credentials and a fully qualified namespace from
    /// configuration.</remarks>
    /// <param name="connectionStringName">The name of the connection string or configuration key used to retrieve Service Bus connection information.
    /// Cannot be null, empty, or whitespace.</param>
    /// <returns>A ServiceBusAdministrationClient instance configured to manage Azure Service Bus resources for the specified
    /// connection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if connectionStringName is null, empty, or consists only of white-space characters.</exception>
    public ServiceBusAdministrationClient CreateServiceBusAdministrationClient(string connectionStringName) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Service Bus ConnectionStringName is required.");
        }

        var serviceBusConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(serviceBusConnection)) {
            return new ServiceBusAdministrationClient(serviceBusConnection);
        }
        serviceBusConnection = _configuration.GetValue<string>(connectionStringName);
        if (!string.IsNullOrWhiteSpace(serviceBusConnection)) {
            return new ServiceBusAdministrationClient(serviceBusConnection);
        }

        var credential = CreateAzureCredential(connectionStringName);
        var namespaceName = _configuration.GetSection(connectionStringName).GetValue<string>("fullyQualifiedNamespace");
        if (string.IsNullOrWhiteSpace(namespaceName)) {
            throw new ArgumentNullException($"\"{connectionStringName}__fullyQualifiedNamespace\" is missing.");
        }
        return new ServiceBusAdministrationClient(namespaceName, credential);
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusAdministrationClient"/> using the provided connection string value directly.
    /// </summary>
    /// <param name="connectionString">The actual Azure Service Bus connection string.</param>
    /// <returns>A <see cref="ServiceBusAdministrationClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public ServiceBusAdministrationClient CreateServiceBusAdministrationClientWithConnectionString(string connectionString) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new ServiceBusAdministrationClient(connectionString);
    }



    private TokenCredential CreateAzureCredential(string connectionStringName) {
        var clientId = _configuration.GetSection(connectionStringName).GetValue<string>("clientId")
            ?? _configuration["AZURE_CLIENT_ID"];

        return string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions {
                ManagedIdentityClientId = clientId
            });
    }
}
