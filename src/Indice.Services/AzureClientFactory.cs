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
public sealed class AzureClientFactory
{
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

        var storageConnection = _configuration.GetConnectionString(connectionStringName) ??
            _configuration.GetValue<string>(connectionStringName);
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
    /// Creates a <see cref="BlobContainerClient"/> instance for the specified connection string name and container name.
    /// </summary>
    /// <param name="connectionStringName">The name of the connection string in the configuration.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <returns>A <see cref="BlobContainerClient"/> instance.</returns>
    public BlobContainerClient CreateBlobContainerClient(string connectionStringName, string containerName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        var storageConnection = _configuration.GetConnectionString(connectionStringName) ??
            _configuration.GetValue<string>(connectionStringName);
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
    /// Creates a <see cref="QueueClient"/> instance for the specified connection string name and queue name.
    /// </summary>
    /// <param name="connectionStringName">The name of the connection string in the configuration.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="options">Optional queue client options.</param>
    /// <returns>A <see cref="QueueClient"/> instance.</returns>
    public QueueClient CreateQueueClient(string connectionStringName, string queueName, QueueClientOptions? options = null) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Storage ConnectionStringName is required.");
        }

        if (string.IsNullOrWhiteSpace(queueName)) {
            throw new ArgumentNullException("Storage queue name is required.");
        }

        var storageConnection = _configuration.GetConnectionString(connectionStringName) ??
            _configuration.GetValue<string>(connectionStringName);
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

        var serviceBusConnection = _configuration.GetConnectionString(connectionStringName) ??
            _configuration.GetValue<string>(connectionStringName);
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

        var serviceBusConnection = _configuration.GetConnectionString(connectionStringName) ??
            _configuration.GetValue<string>(connectionStringName);
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
