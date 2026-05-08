using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace Indice.Services;

public sealed class AzureClientFactory
{
    private IConfiguration _configuration;

    public AzureClientFactory(IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        _configuration = configuration;
    }

    public BlobContainerClient CreateBlobContainerClient(string connectionStringName, string containerName) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Storage connection string name is required.");
        }

        if (string.IsNullOrWhiteSpace(containerName)) {
            throw new ArgumentNullException("Storage container name is required.");
        }

        var storageConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(storageConnection)) {
            return new BlobContainerClient(storageConnection, containerName);
        }

        var credential = CreateAzureCredential(connectionStringName);
        var accountName = _configuration[$"{connectionStringName}__accountName"];
        var blobUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        return new BlobContainerClient(blobUri, credential);
    }

    public QueueClient CreateQueueClient(string connectionStringName, string queueName, QueueClientOptions? options = null) {
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

        var credential = CreateAzureCredential(connectionStringName);
        var accountName = _configuration[$"{connectionStringName}__accountName"];
        var queueUri = new Uri($"https://{accountName}.queue.core.windows.net/{queueName}");
        return new QueueClient(queueUri, credential, options);
    }

    public ServiceBusClient CreateServiceBusClient(string connectionStringName) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Service Bus ConnectionStringName is required.");
        }

        var serviceBusConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(serviceBusConnection)) {
            return new ServiceBusClient(serviceBusConnection);
        }

        var credential = CreateAzureCredential(connectionStringName);
        var namespaceName = _configuration[$"{connectionStringName}__fullyQualifiedNamespace"];

        return new ServiceBusClient(namespaceName, credential);
    }

    public ServiceBusAdministrationClient CreateServiceBusAdministrationClient(string connectionStringName) {
        if (string.IsNullOrWhiteSpace(connectionStringName)) {
            throw new ArgumentNullException("Service Bus ConnectionStringName is required.");
        }

        var serviceBusConnection = _configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(serviceBusConnection)) {
            return new ServiceBusAdministrationClient(serviceBusConnection);
        }

        var credential = CreateAzureCredential(connectionStringName);
        var namespaceName = _configuration[$"{connectionStringName}__fullyQualifiedNamespace"];

        return new ServiceBusAdministrationClient(namespaceName, credential);
    }

    private TokenCredential CreateAzureCredential(string connectionStringName) {
        var clientId = _configuration[$"{connectionStringName}__clientId"] ?? _configuration["AZURE_CLIENT_ID"];

        return string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions {
                ManagedIdentityClientId = clientId
            });
    }
}
