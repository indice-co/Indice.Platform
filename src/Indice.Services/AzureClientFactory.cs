using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Indice.Types;

namespace Indice.Services;

internal static class AzureConnectionKeys
{
    internal static class StorageAccount {
        internal const string AccountName = nameof(AccountName);
        internal const string ContainerName = nameof(ContainerName);
        internal const string EndpointSuffix = nameof(EndpointSuffix);
    }

    internal static class ServiceBus {
        internal const string Endpoint = nameof(Endpoint);
    }
}

/// <summary>
/// Factory for creating configured <see cref="BlobContainerClient"/> instances for Azure Blob Storage.
/// Supports both connection string authentication and managed identity authentication.
/// </summary>
public static class AzureClientFactory
{
    /// <summary>
    /// Creates a <see cref="BlobContainerClient"/> using the container name defined in the connection string.
    /// </summary>
    /// <param name="connectionString">Storage connection configuration.</param>
    /// <returns>A configured <see cref="BlobContainerClient"/> instance.</returns>
    public static BlobContainerClient CreateBlobContainerClient(AzureConnectionString connectionString) {
        var container = connectionString[AzureConnectionKeys.StorageAccount.ContainerName];
        if (string.IsNullOrWhiteSpace(container)) {
            throw new ArgumentNullException("ContainerName property is required in connection string.");
        }

        return CreateBlobContainerClient(connectionString, container);
    }

    /// <summary>
    /// Creates a <see cref="BlobContainerClient"/> for a specific container.
    /// Uses managed identity when enabled, otherwise falls back to connection string authentication.
    /// </summary>
    /// <param name="connectionString">Storage connection configuration.</param>
    /// <param name="containerName">Target blob container name.</param>
    /// <returns>A configured <see cref="BlobContainerClient"/> instance.</returns>
    public static BlobContainerClient CreateBlobContainerClient(AzureConnectionString connectionString, string containerName) {
        if (string.IsNullOrWhiteSpace(containerName)) {
            throw new ArgumentNullException("Container Name is required.");
        }

        if (connectionString.HasManagedIdentity) {
            var accountName = connectionString[AzureConnectionKeys.StorageAccount.AccountName];
            var endpointSuffix = connectionString[AzureConnectionKeys.StorageAccount.EndpointSuffix];

            if (string.IsNullOrWhiteSpace(accountName)) {
                throw new ArgumentNullException("AccountName property is required in connection string.");
            }

            var endpoint = new Uri($"https://{accountName}.blob.{endpointSuffix}/{containerName}");
            var credential = CreateAzureCredential(connectionString);

            return new BlobContainerClient(endpoint, credential);
        }

        return new BlobContainerClient(connectionString.ToString(), containerName);
    }

    /// <summary>
    /// Creates an Azure Storage QueueClient using either Managed Identity or connection string authentication.
    /// </summary>
    /// <param name="connectionString">Storage connection configuration.</param>
    /// <param name="queueName">Target queue name.</param>
    /// <param name="options">Optional client configuration options.</param>
    /// <returns>A configured <see cref="QueueClient"/> instance.</returns>
    public static QueueClient CreateQueueClient(AzureConnectionString connectionString, string queueName, QueueClientOptions? options = null) {
        if (string.IsNullOrWhiteSpace(queueName)) {
            throw new ArgumentNullException("Queue Name is required.");
        }

        if (connectionString.HasManagedIdentity) {
            var accountName = connectionString[AzureConnectionKeys.StorageAccount.AccountName];
            var endpointSuffix = connectionString[AzureConnectionKeys.StorageAccount.EndpointSuffix];

            if (string.IsNullOrWhiteSpace(accountName)) {
                throw new ArgumentNullException("AccountName property is required in connection string.");
            }

            var endpoint = new Uri($"https://{accountName}.queue.{endpointSuffix}/{queueName}");
            var credential = CreateAzureCredential(connectionString);

            return new QueueClient(endpoint, credential, options);
        }

        return new QueueClient(connectionString.ToString(), queueName, options);
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusClient"/> using either Managed Identity authentication
    /// or connection string authentication depending on the provided configuration.
    /// </summary>
    /// <param name="connectionString">Azure connection string configuration.</param>
    /// <returns>A configured <see cref="ServiceBusClient"/> instance.</returns>
    public static ServiceBusClient CreateServiceBusClient(AzureConnectionString connectionString) {
        if (connectionString.HasManagedIdentity) {
            return new ServiceBusClient(GetServiceBusFqdn(connectionString), CreateAzureCredential(connectionString));
        }

        return new ServiceBusClient(connectionString.ToString());
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusAdministrationClient"/> using either Managed Identity authentication
    /// or connection string authentication depending on the provided configuration.
    /// </summary>
    /// <param name="connectionString">Azure connection string configuration.</param>
    /// <returns>A configured <see cref="ServiceBusAdministrationClient"/> instance.</returns>
    public static ServiceBusAdministrationClient CreateServiceBusAdministrationClient(AzureConnectionString connectionString) {
        if (connectionString.HasManagedIdentity) {
            return new ServiceBusAdministrationClient(GetServiceBusFqdn(connectionString), CreateAzureCredential(connectionString));
        }

        return new ServiceBusAdministrationClient(connectionString.ToString());
    }

    /// <summary>
    /// Normalizes a Service Bus endpoint by removing protocol prefixes and trailing slashes,
    /// returning a fully qualified namespace (FQDN).
    /// </summary>
    /// <returns>Normalized Service Bus FQDN.</returns>
    private static string GetServiceBusFqdn(AzureConnectionString connectionString) =>
        connectionString[AzureConnectionKeys.ServiceBus.Endpoint]!
            .Replace("sb://", string.Empty)
            .Replace("https://", string.Empty)
            .TrimEnd('/');

    /// <summary>
    /// Creates a <see cref="TokenCredential"/> for Azure authentication.
    /// Uses system-assigned managed identity when no client ID is provided.
    /// </summary>
    /// <param name="connectionString">The Azure connection string.</param>
    /// <returns>A <see cref="TokenCredential"/> instance.</returns>
    private static TokenCredential CreateAzureCredential(AzureConnectionString connectionString) {
        var clientId = connectionString.UseSystemAssigned
            ? string.Empty
            : connectionString.ManagedIdentityClientId;

        return string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions {
                ManagedIdentityClientId = clientId
            });
    }
}
