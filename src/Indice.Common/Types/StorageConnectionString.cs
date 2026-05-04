namespace Indice.Types;

/// <summary>
/// A specialized <see cref="ConnectionString"/> class for Azure Storage connection strings that can also handle managed identity authentication by including a "ManagedIdentity" property. If the "ManagedIdentity" property is present, it indicates that the connection string should be used with managed identity authentication instead of a traditional connection string.
/// </summary>
public sealed class StorageConnectionString : ConnectionString
{
    /// <summary>
    /// Initializes a new instance of the StorageConnectionString class using the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string used to establish a connection to the storage service. Cannot be null or empty.</param>
    public StorageConnectionString(string connectionString) : base(connectionString) { }

    /// <summary>
    /// Gets the client ID of the user-assigned managed identity to use for authentication.
    /// </summary>
    public string? ManagedIdentityClientId => this[nameof(ManagedIdentityClientId)];

    /// <summary>
    /// Gets a value indicating whether system-assigned managed identity should be used for authentication.
    /// </summary>
    public bool UseSystemAssigned =>
        (ContainsKey(nameof(UseSystemAssigned)) &&
            bool.TryParse(this[nameof(UseSystemAssigned)], out var value) &&
                value);

    /// <summary>
    /// Gets the value of the Storage Account endpoint suffix.
    /// </summary>
    public string? EndpointSuffix => this[nameof(EndpointSuffix)];

    /// <summary>
    /// Gets the name of the Storage Account resource.
    /// </summary>
    public string? AccountName => this[nameof(AccountName)];

    /// <summary>
    /// Gets the name of the blob container.
    /// </summary>
    public string? ContainerName => this[nameof(ContainerName)];

    /// <summary>
    /// Gets a value indicating whether the current configuration uses a managed identity for authentication.
    /// </summary>
    public bool HasManagedIdentity =>
        ContainsKey(nameof(ManagedIdentityClientId)) ||
            (ContainsKey(nameof(UseSystemAssigned)) &&
            bool.TryParse(this[nameof(UseSystemAssigned)], out var value) &&
                value);
}
