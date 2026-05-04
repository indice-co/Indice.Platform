using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Indice.Types;

namespace Indice.Services;

/// <summary>
/// Factory for creating configured <see cref="BlobContainerClient"/> instances for Azure Blob Storage.
/// Supports both connection string authentication and managed identity authentication.
/// </summary>
public static class AzureStorageClientFactory
{
    /// <summary>
    /// Creates a <see cref="BlobContainerClient"/> using the container name defined in the connection string.
    /// </summary>
    /// <param name="connectionString">Storage connection configuration.</param>
    /// <returns>A configured <see cref="BlobContainerClient"/> instance.</returns>
    public static BlobContainerClient CreateBlobContainerClient(StorageConnectionString connectionString) {
        if (string.IsNullOrWhiteSpace(connectionString.ContainerName)) {
            throw new ArgumentNullException("ContainerName property is required in connection string.");
        }

        return CreateBlobContainerClient(connectionString, connectionString.ContainerName);
    }

    /// <summary>
    /// Creates a <see cref="BlobContainerClient"/> for a specific container.
    /// Uses managed identity when enabled, otherwise falls back to connection string authentication.
    /// </summary>
    /// <param name="connectionString">Storage connection configuration.</param>
    /// <param name="containerName">Target blob container name.</param>
    /// <returns>A configured <see cref="BlobContainerClient"/> instance.</returns>
    public static BlobContainerClient CreateBlobContainerClient(StorageConnectionString connectionString, string containerName) {
        if (string.IsNullOrWhiteSpace(containerName)) {
            throw new ArgumentNullException("Container Name is required.");
        }

        if (connectionString.HasManagedIdentity) {
            if (string.IsNullOrWhiteSpace(connectionString.AccountName)) {
                throw new ArgumentNullException("AccountName property is required in connection string.");
            }

            var endpoint = new Uri($"https://{connectionString.AccountName}.blob.{connectionString.EndpointSuffix}/{containerName}");
            var clientId = (connectionString.UseSystemAssigned ? string.Empty : connectionString.ManagedIdentityClientId);
            var credential = CreateCredential(clientId);

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
    public static QueueClient CreateQueueClient(StorageConnectionString connectionString, string queueName, QueueClientOptions? options = null) {
        if (string.IsNullOrWhiteSpace(queueName)) {
            throw new ArgumentNullException("Queue Name is required.");
        }

        if (connectionString.HasManagedIdentity) {
            if (string.IsNullOrWhiteSpace(connectionString.AccountName)) {
                throw new ArgumentNullException("AccountName property is required in connection string.");
            }

            var endpoint = new Uri($"https://{connectionString.AccountName}.queue.{connectionString.EndpointSuffix}/{queueName}");
            var clientId = (connectionString.UseSystemAssigned ? string.Empty : connectionString.ManagedIdentityClientId);
            var credential = CreateCredential(clientId);

            return new QueueClient(endpoint, credential, options);
        }

        return new QueueClient(connectionString.ToString(), queueName, options);
    }

    /// <summary>
    /// Creates a <see cref="TokenCredential"/> for Azure authentication.
    /// Uses system-assigned managed identity when no client ID is provided.
    /// </summary>
    /// <param name="clientId">Optional user-assigned managed identity client ID.</param>
    /// <returns>A <see cref="TokenCredential"/> instance.</returns>
    private static TokenCredential CreateCredential(string? clientId) {
        if (string.IsNullOrWhiteSpace(clientId)) {
            return new DefaultAzureCredential();
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions {
            ManagedIdentityClientId = clientId
        });
    }
}
