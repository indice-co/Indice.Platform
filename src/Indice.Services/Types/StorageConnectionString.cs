namespace Indice.Types;

/// <summary>
/// A specialized <see cref="ConnectionString"/> class for Azure Storage connection strings that can also handle managed identity authentication by including a "ManagedIdentity" property. If the "ManagedIdentity" property is present, it indicates that the connection string should be used with managed identity authentication instead of a traditional connection string.
/// </summary>
public class StorageConnectionString : ConnectionString
{
    /// <summary>
    /// Initializes a new instance of the StorageConnectionString class using the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string used to establish a connection to the storage service. Cannot be null or empty.</param>
    public StorageConnectionString(string connectionString) : base(connectionString) { }

    /// <summary>
    /// Gets the client ID of the managed identity to use for authentication.
    /// </summary>
    public string? ManagedIdentityId => this[nameof(ManagedIdentityId)];

    /// <summary>
    /// Gets a value indicating whether the current configuration uses a managed identity for authentication.
    /// </summary>
    public bool HasManagedIdentity => this.ContainsKey(nameof(ManagedIdentityId));

    /// <summary>
    /// Returns a standard connection string representation suitable for use with authentication methods that do not
    /// require managed identity.
    /// </summary>
    /// <remarks>Use this method to obtain a connection string compatible with authentication
    /// scenarios that do not utilize managed identity. When managed identity is enabled, the returned connection
    /// string omits managed identity parameters, as they are not required for authentication.</remarks>
    /// <returns>A connection string formatted for standard authentication. If managed identity is used, returns a connection
    /// string with managed identity information removed.</returns>
    public string ToStandardConnectionString() {
        if (HasManagedIdentity) {
            // Return a dummy connection string for managed identity, as the actual connection string is not needed for authentication.
            var copy = new ConnectionString(this);
            copy.Remove(nameof(ManagedIdentityId));
            return copy.ToString();
        }
        return ToString();
    }
}
